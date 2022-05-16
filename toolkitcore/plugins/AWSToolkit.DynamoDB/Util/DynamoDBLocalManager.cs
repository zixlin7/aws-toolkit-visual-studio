using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Linq;
using System.Diagnostics;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Util;

using log4net;

namespace Amazon.AWSToolkit.DynamoDB.Util
{
    public class DynamoDBLocalManager
    {
        public enum CurrentState { Stopped, Started, Connected };
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(DynamoDBLocalManager));

        const string MANIFEST_KEY = "manifest.xml";
        const string BASE_BUCKET_URL = "https://aws-toolkits-dynamodb-local.s3.amazonaws.com/";

        static readonly string BASE_LOCAL_DIRECTORY = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dynamodb-local");

        private static DynamoDBLocalManager _instance = new DynamoDBLocalManager();

        private DynamoDBLocalManager()
        { }

        ~DynamoDBLocalManager()
        {
            try
            {
                if (this._javaProcess != null && this._state != CurrentState.Stopped)
                {
                    this._javaProcess.Kill();
                }
            }
            catch { }
        }

        public static DynamoDBLocalManager Instance => _instance;

        public bool IsRunning => this._javaProcess != null && this._state != CurrentState.Stopped;

        CurrentState _state = CurrentState.Stopped;
        public CurrentState State
        {
            get
            {
                return this._state;
            }
        }

        public IEnumerable<DynamoDBLocalManager.DynamoDBLocalVersion> GetAvailableVersions()
        {
            try
            {
                string manifestContent = ManifestContent();
                if (manifestContent == null)
                    return new List<DynamoDBLocalManager.DynamoDBLocalVersion>();

                XDocument xdoc = XDocument.Parse(manifestContent);
                var query = from p in xdoc.Elements("manifest").Elements("version")
                            select new DynamoDBLocalVersion(p.Element("name").Value.Trim(), p.Element("description").Value.Trim(), p.Element("key").Value.Trim(), false);

                var list = new List<DynamoDBLocalVersion>();
                foreach (var version in query.Reverse())
                {
                    var jarFile = Path.Combine(BASE_LOCAL_DIRECTORY, version.Version, "DynamoDBLocal.jar");
                    if (File.Exists(jarFile))
                        version.IsInstalled = true;

                    list.Add(version);
                }

                return list;
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to load available dynamodb versions", e);
                return new List<DynamoDBLocalManager.DynamoDBLocalVersion>();
            }
        }

        #region Install / Uninstall
        public delegate void DownloadProgress(object sender, DownloadProgressEventArgs args);

        public void InstallAsync(DynamoDBLocalVersion version, DownloadProgress callback)
        {
            string manifestContent = ManifestContent();

            if (!Directory.Exists(BASE_LOCAL_DIRECTORY))
                Directory.CreateDirectory(BASE_LOCAL_DIRECTORY);

            File.WriteAllText(Path.Combine(BASE_LOCAL_DIRECTORY, MANIFEST_KEY), manifestContent);

            string localZipFile = Path.Combine(BASE_LOCAL_DIRECTORY, version.Key);
            if (File.Exists(localZipFile))
                File.Delete(localZipFile);

            var progressEventArgs = new DownloadProgressEventArgs();
            WebClient client = new WebClient();
            client.DownloadProgressChanged += (DownloadProgressChangedEventHandler)((x, e) => 
                {
                    progressEventArgs.BytesReceived = e.BytesReceived;
                    progressEventArgs.TotalBytesReceived = e.TotalBytesToReceive;
                    callback(this, progressEventArgs);
                });

            client.DownloadFileCompleted += (AsyncCompletedEventHandler)((x, e) =>
                {
                    progressEventArgs.Complete = true;
                    try
                    {
                        if (e.Error != null)
                        {
                            progressEventArgs.Error = e.Error;
                            return;
                        }

                        try
                        {
                            var targetDirectory = Path.Combine(BASE_LOCAL_DIRECTORY, version.Version);
                            ZipUtil.ExtractZip(zipFile: localZipFile, destFolder: targetDirectory, overwriteFiles: true);
                            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() => version.IsInstalled = true));

                            File.Delete(localZipFile);
                        }
                        catch (Exception ex)
                        {
                            progressEventArgs.Error = ex;
                        }
                    }
                    finally
                    {
                        callback(this, progressEventArgs);
                    }
                });

            client.DownloadFileAsync(new Uri(string.Concat(BASE_BUCKET_URL, version.Key)), localZipFile);           
        }

        public void Uninstall(DynamoDBLocalVersion version)
        {
            var localFolder = Path.Combine(BASE_LOCAL_DIRECTORY, version.Version);
            if (Directory.Exists(localFolder))
            {
                Directory.Delete(localFolder, true);
            }

            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() => version.IsInstalled = false));
        }

        private string ManifestContent()
        {
            string content = null;
            try
            {
                WebClient client = new WebClient();
                content = client.DownloadString(string.Concat(BASE_BUCKET_URL, MANIFEST_KEY));
            }
            catch (Exception e)
            {
                LOGGER.Warn("Failed to get manifest from S3, falling back to local copy", e);
            }

            if (content == null)
            {
                content = File.ReadAllText(Path.Combine(BASE_LOCAL_DIRECTORY, MANIFEST_KEY));
            }

            return content;
        }

        public class DownloadProgressEventArgs
        {
            public long BytesReceived
            {
                get;
                set;
            }

            public long TotalBytesReceived
            {
                get;
                set;
            }

            public bool Complete
            {
                get;
                set;
            }

            public Exception Error
            {
                get;
                set;
            }
        }
        #endregion

        #region Start / Stop

        public int LastConfiguredPort
        {
            get
            {
                return ToolkitSettings.Instance.DynamoDb.Port;
            }
        }

        Process _javaProcess;
        public void Start(DynamoDBLocalVersion version, int port, string javaExe)
        {
            string javaOverrideExe = javaExe;
            string javaWExe = Path.Combine(Path.GetDirectoryName(javaExe), "javaw.exe");
            if (File.Exists(javaWExe))
                javaOverrideExe = javaWExe;

            PersistLastConfiguredPort(port);
            this._javaProcess = new Process();
            this._javaProcess.StartInfo.FileName = javaOverrideExe;
            this._javaProcess.StartInfo.WorkingDirectory = Path.Combine(BASE_LOCAL_DIRECTORY, version.Version);
            this._javaProcess.StartInfo.Arguments = string.Format("-jar DynamoDBLocal.jar --port {0}", port);
            this._javaProcess.StartInfo.UseShellExecute = false;
            this._javaProcess.StartInfo.RedirectStandardOutput = true;
            this._javaProcess.StartInfo.RedirectStandardError = true;

            this._javaProcess.Exited += new EventHandler(_javaProcess_Exited);
            this._javaProcess.EnableRaisingEvents = true;

            ToolkitFactory.Instance.ShellProvider.OutputToHostConsole("DynamoDB Local Starting", true);
            if (this._javaProcess.Start())
            {
                Redirect(this._javaProcess.StandardOutput);
                Redirect(this._javaProcess.StandardError);
                this._state = CurrentState.Started;
            }
        }

        public event EventHandler StartedJavaProcessExited;
        void _javaProcess_Exited(object sender, EventArgs e)
        {
            if (sender.Equals(_javaProcess))
            {
                this._state = CurrentState.Stopped;
                this._javaProcess = null;
            }

            if (!(sender is Process process))
            {
                return;
            }

            process.Dispose();

            if (StartedJavaProcessExited != null)
            {
                ToolkitFactory.Instance.ShellProvider.OutputToHostConsole("DynamoDB Local has exited", true);
                StartedJavaProcessExited(this, new EventArgs());
            }
        }

        private void Redirect(StreamReader input)
        {
            new Thread(a =>
            {
                var buffer = new char[1];
                string line;
                while ((line = input.ReadLine()) != null)
                {
                    ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(line, true);
                };                
            }).Start();
        }

        public void Stop()
        {
            if (this._javaProcess != null)
            {
                if (this._state != CurrentState.Stopped)
                {
                    this._javaProcess.Kill();
                }

                this._javaProcess = null;
            }

            this._state = CurrentState.Stopped;
        }

        public void Connect(int port)
        {
            this.Stop();

            this._state = CurrentState.Connected;
            PersistLastConfiguredPort(port);
        }

        private void PersistLastConfiguredPort(int port)
        {
            ToolkitSettings.Instance.DynamoDb.Port = port;
        }


        #endregion

        public class DynamoDBLocalVersion : Amazon.AWSToolkit.CommonUI.BaseModel
        {
            public DynamoDBLocalVersion(string version, string description, string key, bool isInstalled)
            {
                this.Version = version;
                this.Description = description;
                this.Key = key;
                this.IsInstalled = isInstalled;
            }

            public string Version
            {
                get;
            }

            public string Description
            {
                get;
            }

            public string Key
            {
                get;
            }

            bool _isInstalled;
            public bool IsInstalled
            {
                get => this._isInstalled;
                set
                {
                    this._isInstalled = value;
                    base.NotifyPropertyChanged("IsInstalled");
                }
            }
        }
    }
}
