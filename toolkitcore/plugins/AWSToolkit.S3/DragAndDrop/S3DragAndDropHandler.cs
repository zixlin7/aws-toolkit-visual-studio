using System;
using System.IO;
using System.Linq;
using System.Threading;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Jobs;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.View;
using Amazon.S3;

using log4net;

namespace Amazon.AWSToolkit.S3.DragAndDrop
{
    /// <summary>
    /// When an object is dragged from the S3 browser to a windows explorer window, this uses the Windows
    /// drag and drop system. The drag and drop system is composed of a "drop source" and a "drop target".
    /// We are responsible for and manage the drop source, but we don't know anything about the drop target --
    /// handling where the objects are dropped is the responsibility of the component being dropped to (in
    /// this use-case, that is Windows Explorer).
    /// 
    /// A temporary proxy file (see <see cref="DropSourcePlaceholder"/>) representing a request to transfer a set
    /// of objects from S3 is produced as the drop source. This file is placed in the system temp folder.
    /// If a user drags and drops S3 objects to a Windows folder, Windows then copies the proxy file into the
    /// destination folder. We give the proxy file a unique name so that we can listen for the creation of this file,
    /// which occurs when the proxy file is copied into the drop-destination folder (note: only local drives
    /// are supported). This file creation event is used to determine where to download S3 objects, and to trigger
    /// the actual S3 object downloads.
    ///
    /// This class is responsible for:
    /// - producing the temporary proxy file
    /// - listening for the proxy file to be copied as a result of a drop
    /// - kicking off the S3 object downloads
    ///
    /// Each instance of this class is intended to handle only the drag and drop request it was provided,
    /// and only the first time a file creation is detected. This prevents re-drive attempts that could be
    /// caused by hand-crafting a file, or by copying the proxy file into multiple folders.
    ///
    /// ----------
    ///
    /// The alternative approach would be to set up an "async drag and drop" operation. This would involve
    /// downloading the S3 objects to a temporary location, and then letting the Windows drag and drop system
    /// move/copy those objects out of the temporary location. This approach wasn't chosen because we don't want
    /// download objects unless necessary, and only into the final location (eg: avoid the chance of leaving downloaded
    /// S3 objects in the temp folder).
    /// </summary>
    internal class S3DragAndDropHandler : IS3DragAndDropHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(S3DragAndDropHandler));

        private const int ManifestLoadMaxAttempts = 5;
        private const int ManifestLoadRetryTimeoutMs = 333;

        private readonly ToolkitContext _toolkitContext;

        private readonly S3DragAndDropRequest _dragDropRequest;
        private readonly S3DropRequestWatcher _dropRequestWatcher;
        private readonly DropSourcePlaceholder _dropSourcePlaceholder;

        public S3DragAndDropHandler(S3DragAndDropRequest dragDropRequest, ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            _dragDropRequest = dragDropRequest;

            var manifestFilename = $"aws-toolkit.{dragDropRequest.RequestId}.s3";
            _dropSourcePlaceholder = new DropSourcePlaceholder(Path.Combine(Path.GetTempPath(), manifestFilename));

            _dropRequestWatcher = new S3DropRequestWatcher(manifestFilename, _dropSourcePlaceholder.Path);
            _dropRequestWatcher.DropRequest += Watcher_DropRequest;
        }

        private void Watcher_DropRequest(object sender, S3DropRequestEventArgs e)
        {
            var watcher = sender as S3DropRequestWatcher;
            if (watcher == null) { return; }

            // Do not handle re-drive attempts (see comments in class header)
            watcher.DropRequest -= Watcher_DropRequest;

            if (CanProcess(e.FilePath))
            {
                ProcessDragAndDrop(e.FilePath);
            }
        }

        private bool CanProcess(string manifestPath)
        {
            try
            {
                if (string.IsNullOrEmpty(manifestPath))
                {
                    return false;
                }

                if (!File.Exists(manifestPath))
                {
                    return false;
                }

                if (!(Directory.GetParent(manifestPath)?.Exists ?? false))
                {
                    return false;
                }

                if (_dragDropRequest.ConnectionSettings?.CredentialIdentifier == null ||
                    _dragDropRequest.ConnectionSettings?.Region == null)
                {
                    return false;
                }

                // Don't process other requests (this also reduces the chance of fulfilling a hand-crafted manifest)
                if (!TryLoadManifest(manifestPath, out var manifest) ||
                    manifest.RequestId != _dragDropRequest.RequestId)
                {
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.Error("Unsupported S3 drag and drop scenario", e);

                return false;
            }
        }

        private static bool TryLoadManifest(string path, out S3DragDropManifest manifest)
        {
            for (int attempt = 0; attempt < ManifestLoadMaxAttempts; attempt++)
            {
                try
                {
                    File.Open(path, FileMode.Open, FileAccess.Read).Dispose();
                    manifest = S3DragDropManifest.Load(path);

                    return true;
                }
                catch (IOException)
                {
                    // We react to file create events.
                    // The IOException "The process cannot access the file 'foo' because it is being used by another process."
                    // can come up, because the file's contents are still being written out.
                    // 
                    // Retry a few times, to see if the file writes complete.
                    Thread.Sleep(ManifestLoadRetryTimeoutMs);
                }
            }

            manifest = null;
            return false;
        }

        /// <summary>
        /// Sets up the job that will copy S3 objects to the folder that was dropped on
        /// </summary>
        public void ProcessDragAndDrop(string manifestPath)
        {
            try
            {
                IAmazonS3 s3Client = CreateS3Client(_dragDropRequest.ConnectionSettings);

                if (s3Client == null)
                {
                    _logger.Error("Unable to handle S3 drag and drop: no S3 client available");
                    return;
                }

                var destFolder = Directory.GetParent(manifestPath).FullName;

                var downloadFilesJob = CreateDownloadFilesJob(s3Client, destFolder);

                // Bring up a dialog that shows the transfer progress
                _toolkitContext.ToolkitHost.BeginExecuteOnUIThread(() =>
                {
                    var feedbackWindow = new DnDCopyProgress();
                    feedbackWindow.ShowInTaskbar = true;
                    feedbackWindow.FinalPrepAndShow(downloadFilesJob);
                });

                // Perform the S3 object downloads
                downloadFilesJob.StartJob();
            }
            catch (Exception e)
            {
                _logger.Error("Unable to perform S3 drag and drop", e);
            }
            finally
            {
                // Remove the "drop target" manifest
                RemoveFile(manifestPath);

                // Remove the "drop source" manifest (the drag and drop operation is done, and they aren't re-used)
                _dropSourcePlaceholder.Dispose();
            }
        }

        private IAmazonS3 CreateS3Client(AwsConnectionSettings connectionSettings)
        {
            return _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonS3Client>(
                connectionSettings.CredentialIdentifier, connectionSettings.Region);
        }

        private DownloadFilesJob CreateDownloadFilesJob(IAmazonS3 s3Client, string destinationFolder)
        {
            var s3Objects = _dragDropRequest.Items
                .Select(item =>
                {
                    var childItemType = (BucketBrowserModel.ChildType) Enum.Parse(
                        typeof(BucketBrowserModel.ChildType), item.ItemType);
                    return new BucketBrowserModel.ChildItem(item.Key, childItemType);
                }).ToArray();

            var controller = new BucketBrowserController(_toolkitContext, s3Client,
                new BucketBrowserModel(_dragDropRequest.BucketName));

            return new DownloadFilesJob(controller, _dragDropRequest.BucketName,
                _dragDropRequest.BaseBucketPath, s3Objects, destinationFolder);
        }

        private void RemoveFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Unable to remove drag and drop manifest: {path}", e);
            }
        }

        public void WriteDropSourcePlaceholder()
        {
            var manifest = new S3DragDropManifest() { RequestId = _dragDropRequest.RequestId, };
            _dropSourcePlaceholder.Create(manifest);
        }

        public string GetDropSourcePath() => _dropSourcePlaceholder.Path;

        public void Dispose()
        {
            _dropRequestWatcher.Dispose();
            _dropSourcePlaceholder.Dispose();
        }
    }
}
