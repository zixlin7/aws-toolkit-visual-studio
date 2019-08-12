using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Amazon.S3;
using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Jobs;
using Amazon.AWSToolkit.S3.View;
using log4net;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class ManifestWatcher
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(ManifestWatcher));

        public const string MANIFEST_FILE_NAME = "s3MaKeys.aws";
        public string INSTANCE_IDENTIFIER = Guid.NewGuid().ToString();

        static ManifestWatcher _instance = new ManifestWatcher();

        string _tempPath;
        Dictionary<string, FileSystemWatcher> _watchers = new Dictionary<string, FileSystemWatcher>();

        private ManifestWatcher()
        {
            this._tempPath = Path.GetTempPath();            
        }

        public static ManifestWatcher Instance => _instance;

        public void Start()
        {
            lock (this._watchers)
            {
                try
                {
                    foreach (var drive in System.IO.DriveInfo.GetDrives())
                    {
                        Console.WriteLine(drive.DriveType);
                        if (DriveType.Fixed != drive.DriveType || !drive.IsReady)
                            continue;

                        string rootDirectory = drive.RootDirectory.Name;
                        if (this._watchers.ContainsKey(rootDirectory))
                            continue;

                        FileSystemWatcher watcher = new FileSystemWatcher(rootDirectory, ManifestWatcher.MANIFEST_FILE_NAME);
                        watcher.IncludeSubdirectories = true;
                        watcher.Created += new FileSystemEventHandler(onCreated);
                        watcher.EnableRaisingEvents = true;
                        this._watchers[rootDirectory] = watcher;
                    }
                }
                catch
                {
                    // TODO: This needs to be logged but this is not a catastrophic event.
                }
            }
        }

        public void Shutdown()
        {
            lock (this._watchers)
            {
                foreach (FileSystemWatcher watcher in this._watchers.Values)
                {
                    watcher.EnableRaisingEvents = false;
                }

                this._watchers.Clear();
            }
        }

        void onCreated(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.StartsWith(this._tempPath))
            {
                return;
            }

            Console.WriteLine("{0} : {1}", DateTime.Now.Ticks, e.FullPath);
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.processManifest), e.FullPath);
        }

        void processManifest(object state)
        {
            Thread.Sleep(25);
            try
            {
                string fullPath = state as string;
                if (string.IsNullOrEmpty(fullPath))
                    return;

                string bucket = null;
                List<BucketBrowserModel.ChildItem> items = new List<BucketBrowserModel.ChildItem>();
                string relativePath = null;
                if (File.Exists(fullPath))
                {
                    IAmazonS3 s3Client = null;
                    using (StreamReader reader = new StreamReader(fullPath))
                    {
                        reader.ReadLine();// Skip Message;

                        string instanceIdentifier = reader.ReadLine();
                        // If they are not equal that means another instance of the application
                        // did the DnD.
                        if (!INSTANCE_IDENTIFIER.Equals(instanceIdentifier))
                            return;

                        string accountUniqueName = reader.ReadLine();
                        string endpointUrl = reader.ReadLine();
                        s3Client = buildS3Client(accountUniqueName, endpointUrl);

                        bucket = reader.ReadLine();
                        relativePath = reader.ReadLine();
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] tokens = line.Split('\t');
                            BucketBrowserModel.ChildType type = (BucketBrowserModel.ChildType)Enum.Parse(typeof(BucketBrowserModel.ChildType), tokens[0].Trim());
                            string key = tokens[1].Trim();
                            items.Add(new BucketBrowserModel.ChildItem(key, type));
                        }
                    }
                    File.Delete(fullPath);
                    if (s3Client == null)
                        return;

                    var controller = new BucketBrowserController(s3Client, new BucketBrowserModel(bucket));
                    DownloadFilesJob job = new DownloadFilesJob(controller, bucket, relativePath, items.ToArray(), new FileInfo(fullPath).DirectoryName);

                    ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
                    {
                        DnDCopyProgress feedbackWindow = new DnDCopyProgress();
                        feedbackWindow.ShowInTaskbar = true;
                        feedbackWindow.FinalPrepAndShow(job);
                    }));

                    job.StartJob();
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error copying processing manifest " + state, e);
            }
        }

        private IAmazonS3 buildS3Client(string accountUniqueName, string endpointUrl)
        {
            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RegisteredProfiles);
            var os = settings[accountUniqueName];
            string accessKey = os[ToolkitSettingsConstants.AccessKeyField];
            string secretKey = os[ToolkitSettingsConstants.SecretKeyField];

            var config = S3Utils.BuildS3Config(endpointUrl);
            IAmazonS3 s3Client = new AmazonS3Client(accessKey, secretKey, config);
            return s3Client;
        }
    }
}
