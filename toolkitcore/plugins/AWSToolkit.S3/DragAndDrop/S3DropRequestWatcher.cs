using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using log4net;

namespace Amazon.AWSToolkit.S3.DragAndDrop
{
    /// <summary>
    /// Raised by <see cref="S3DropRequestWatcher"/> when a drag and drop manifest has been created as the
    /// result dropping S3 objects into a Windows folder.
    /// </summary>
    internal class S3DropRequestEventArgs : EventArgs
    {
        public string FilePath { get; set; }
    }

    /// <summary>
    /// Watches the filesystem for a single S3 drag and drop operation. Raises an event if objects were
    /// dropped, resulting in the proxy file being copied into the destination folder.
    /// <see cref="S3DragAndDropHandler"/> for details.
    ///
    /// File watchers are deactivated after the first matching file event is found.
    /// </summary>
    internal class S3DropRequestWatcher : IDisposable
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(S3DropRequestWatcher));

        public event EventHandler<S3DropRequestEventArgs> DropRequest;

        private readonly object _syncRoot = new object();
        private volatile bool _handled = false;

        private readonly List<FileSystemWatcher> _fileWatchers = new List<FileSystemWatcher>();
        private readonly string _dropSourcePath;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileFilter">The unique drag and drop proxy manifest filename to watch for</param>
        /// <param name="dropSourcePath">The location of the original drop source (eg: where drag and drop will copy the file from)</param>
        public S3DropRequestWatcher(string fileFilter, string dropSourcePath)
        {
            _dropSourcePath = dropSourcePath;

            CreateFileWatchers(fileFilter);
        }

        private void CreateFileWatchers(string fileFilter)
        {
            try
            {
                // Watch all local drives (all subfolders from the root) for the specific filename.
                var watchers = GetDrivesToWatch()
                    .Select(drive => drive.RootDirectory.Name)
                    .Select(rootDirectory =>
                    {
                        var watcher = new FileSystemWatcher(rootDirectory, fileFilter);
                        watcher.IncludeSubdirectories = true;
                        watcher.Created += FileWatcher_FileCreated;
                        watcher.EnableRaisingEvents = true;

                        return watcher;
                    });

                _fileWatchers.AddRange(watchers);
            }
            catch (Exception e)
            {
                _logger.Error(
                    "Unable to set up file watchers for the S3 Drag and Drop manifest." +
                    " Drag and Drop from the S3 browser to a windows folder may not work.", e);
            }
        }

        private IEnumerable<DriveInfo> GetDrivesToWatch() =>
            DriveInfo.GetDrives()
                .Where(drive => drive.DriveType == DriveType.Fixed)
                .Where(drive => drive.IsReady);

        /// <summary>
        /// Raised when S3 objects are "dropped" onto a folder, which creates a copy of the drop source proxy manifest.
        /// </summary>
        void FileWatcher_FileCreated(object sender, FileSystemEventArgs e)
        {
            // In case multiple events are raised concurrently, let the first one pass through
            lock (_syncRoot)
            {
                if (_handled)
                {
                    return;
                }

                if (e.FullPath.Equals(_dropSourcePath, StringComparison.InvariantCultureIgnoreCase))
                {
                    return;
                }

                _handled = true;
            }

            DisableFileWatcherEvents();

            var dropRequest = new S3DropRequestEventArgs() { FilePath = e.FullPath };

            // Handle callbacks in a separate thread
            ThreadPool.QueueUserWorkItem(OnDropRequest, dropRequest);
        }

        void OnDropRequest(object state)
        {
            if (state is S3DropRequestEventArgs dropRequest)
            {
                DropRequest?.Invoke(this, dropRequest);
            }
        }

        public void Dispose()
        {
            DisableFileWatcherEvents();

            _fileWatchers.ForEach(watcher => watcher.Dispose());
            _fileWatchers.Clear();
        }

        private void DisableFileWatcherEvents()
        {
            _fileWatchers.ForEach(watcher => watcher.EnableRaisingEvents = false);
        }
    }
}
