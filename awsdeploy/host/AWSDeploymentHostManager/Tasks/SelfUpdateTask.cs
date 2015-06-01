using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Reflection;
using System.IO.Compression;
using System.Diagnostics;

using log4net;

namespace AWSDeploymentHostManager.Tasks
{
    public class SelfUpdateTask : Task
    {
        private enum CustomCommand
        {
            StartHostManager = 128
        }

        const string VERSION_HEADER = "x-amz-meta-hostmanagerversion";
        static readonly string HOST_MANAGER_URL;

        const string HOST_MANAGER_URL_KEY = "hostManagerUrl";

        static ILog LOGGER = LogManager.GetLogger(typeof(SelfUpdateTask));
        static readonly IDictionary<string, string> FINAL_EVENT_PARAMETER;
        string _tempFileLocation = Path.GetTempFileName() + ".zip";
        string _s3Url;
        string _digest;
        const string DATETIME_FORMAT_STRING = "yyyy-MM-ddTHH-mm-ssZ"; // can't use HH:MM:SS as folder name :-)
        const string HM_FOLDERNAME_TEMPLATE = "HostManager.{0}";

        static SelfUpdateTask()
        {
            FINAL_EVENT_PARAMETER = new Dictionary<string, string>();
            FINAL_EVENT_PARAMETER["FinalEvent"] = "true";

            HOST_MANAGER_URL = ConfigurationSettings.AppSettings["HostManagerUpdateSite"];
        }

        public override string Operation
        {
            get
            {
                return "SelfUpdate";
            }
        }

        public string S3Url
        {
            get { return this._s3Url; }
            set { this._s3Url = value; }
        }

        public override string Execute()
        {
            Interlocked.Increment(ref HostManager.ASyncTasksRunning);
            ThreadPool.QueueUserWorkItem(this.DoExecute);
            return GenerateResponse(TASK_RESPONSE_DEFER);
        }

        public void CheckForNewVersionAndUpdate()
        {
            if (IsUpdateAvailable())
            {
                this.SetParameter(HOST_MANAGER_URL_KEY, HOST_MANAGER_URL);
                this.DoExecute(null);
            }
        }

        public void DoExecute(object state)
        {
            try
            {
                bool forceRestart = false;

                if (!this.parameters.TryGetValue(HOST_MANAGER_URL_KEY, out this._s3Url))
                {
                    LOGGER.Error("Parameter missing in request: hostManagerUrl");
                    Event.LogEvent(Operation, "Parameter missing in request: hostManagerUrl", Event.EVENT_SEVERITY_WARN, FINAL_EVENT_PARAMETER);
                    return;
                }

                // noticed that SelfUpdate calls do not supply a digest currently, so treat as optional
                this.parameters.TryGetValue("digest", out this._digest);

                try
                {
                    S3Util.WriteToFile(this._s3Url, this._tempFileLocation);
                    if (S3Util.VerifyFileDigest(this._tempFileLocation, this._digest))
                    {
                        deployHostManagerUpdate();
                        LOGGER.InfoFormat("SelfUpdate Completed. (S3Url: {0})", this._s3Url);
                        Event.LogEvent(Operation, "SelfUpdate Completed", Event.EVENT_SEVERITY_INFO, FINAL_EVENT_PARAMETER);

                        forceRestart = true;
                    }
                    else
                    {
                        Event.LogEvent(Operation, "Digest mismatch failure", Event.EVENT_SEVERITY_WARN, FINAL_EVENT_PARAMETER);
                    }
                }
                catch (Exception e)
                {
                    LOGGER.Error(string.Format("Exception self update. (S3Url: {0})", this._s3Url), e);
                    Event.LogEvent(Operation, String.Format("SelfUpdate failed with exception: {0}", e.Message), Event.EVENT_SEVERITY_WARN, FINAL_EVENT_PARAMETER);
                }
                finally
                {

                    try
                    {
                        if (File.Exists(this._tempFileLocation))
                            File.Delete(this._tempFileLocation);
                    }
                    catch (Exception) { /* provided we extracted the file, no big deal if we fail to clean it up */ }

                    if (forceRestart)
                    {
                        // Wait one second for the old process to clean up then force a shutdown.
                        ThreadPool.QueueUserWorkItem(this.cleanUpProcesses);
                        Thread.Sleep(1000);

                        try
                        {
                            RestartProcess("Harp String");
                            Thread.Sleep(5 * 1000); // Give HostManagerApp some extra time to get the pipe setup.
                            RestartProcess("Magic Harp");

                        }
                        catch (Exception e)
                        {
                            LOGGER.Error("Error existing old processes after upgrade.", e);
                        }
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref HostManager.ASyncTasksRunning);
            }
        }

        void deployHostManagerUpdate()
        {
            DirectoryInfo parentDir 
                = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).Parent;

            string versionFolderSuffix = DateTime.UtcNow.ToString(DATETIME_FORMAT_STRING);
            DirectoryInfo targetDir = parentDir.CreateSubdirectory(string.Format(HM_FOLDERNAME_TEMPLATE, versionFolderSuffix));

            LOGGER.InfoFormat("Extracting new host manager to {0}", targetDir.FullName);
            decompressArchive(this._tempFileLocation, targetDir.FullName);

            makeSureNewHostManagerCanRun(targetDir);
        }

        void makeSureNewHostManagerCanRun(DirectoryInfo targetDir)
        {
            bool ready = false;
            targetDir.Refresh();
            FileInfo[] targetExe = targetDir.GetFiles("AWSDeploymentHostManager.exe");
            if (targetExe.Length == 1)
            {
                Process test = null;
                try
                {
                    LOGGER.DebugFormat("Testing host manager {0}", targetExe[0]);
                    test = Process.Start(targetExe[0].FullName, "test");
                }
                catch (Exception e)
                {
                    Event.LogWarn(this.Operation, "New Hostmanger not able to be lanuched: " + e.Message);
                    LOGGER.Warn("New Hostmanger not able to be lanuched.");
                }
                finally
                {
                    if (!test.HasExited)
                    {
                        test.Kill();
                        ready = true;
                    }
                }
            }
            else
            {
                LOGGER.ErrorFormat("Failed to find host manager in new folder: {0}", targetExe.Length);
            }

            if (!ready)
            {
                // delete the target so we don't confuse Harp service on next restart
                targetDir.Delete();
                throw new Exception(string.Format("HostManager update archive {0} into target folder {1} failed.  Reverting to previous HostManager.",
                                                  this._tempFileLocation,
                                                  targetDir.FullName));
            }
        }

        void cleanUpProcesses(object state) 
        {
            try
            {
                if (PipeHost.instance != null)
                {
                    PipeHost.instance.GetReadyForExit(HostManagerStatus.Queue);
                };
                Interlocked.Decrement(ref HostManager.ASyncTasksRunning);
                HostManager.instance.Exit();
            }
            catch (Exception e)
            {
                LOGGER.Info("Failed to clean old process after upgrading.", e);
            }
        }

        /// <summary>
        /// Extracts the contents of a zip file to the specified folder
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="targetFolder"></param>
        public static void decompressArchive(string archive, string targetFolder)
        {
            Shell32.Shell sc = null;
            Shell32.Folder SrcFlder = null;
            Shell32.Folder DestFlder = null;
            Shell32.FolderItems items = null;

            try
            {
                sc = new Shell32.ShellClass();

                SrcFlder = sc.NameSpace(archive);
                DestFlder = sc.NameSpace(targetFolder);
                items = SrcFlder.Items();
                DestFlder.CopyHere(items, 20);
            }
            catch (Exception e)
            {
                LOGGER.Error(string.Format("Exception self update. 'decompressArchive' caught {0}", e.Message), e);
            }
            finally
            {
                // attempt to release shell resources to see if we can then delete the tempfile
                DestFlder = null;
                items = null;
                SrcFlder = null;
                sc = null;
            }
        }

        /// <summary>
        /// Check for a new version of the host manager.
        /// </summary>
        /// <returns>True if a new version is available</returns>
        bool IsUpdateAvailable()
        {
            if (string.IsNullOrWhiteSpace(HOST_MANAGER_URL))
                return false;

            try
            {
                LOGGER.DebugFormat("Checking for host manager update at {0}", HOST_MANAGER_URL);

                var request = WebRequest.Create(HOST_MANAGER_URL) as HttpWebRequest;
                request.Method = "HEAD";

                using (var response = request.GetResponse())
                {
                    string version = response.Headers[VERSION_HEADER];
                    if (string.IsNullOrEmpty(version))
                        return false;

                    
                    string currentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
                    if (!currentVersion.Equals(version))
                    {
                        LOGGER.InfoFormat("New version {0} is available.", version);                        
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception e)
            {
                LOGGER.Info("Error checking for new version of the host manager.", e);
                return false;
            }
        }

        private static void RestartProcess(string serviceName)
        {
            ServiceController controller = new ServiceController(serviceName);

            if (controller.Status != ServiceControllerStatus.Running)
            {
                LOGGER.InfoFormat("{0} was not running, waiting for Magic Harp to start", serviceName);
                controller.WaitForStatus(ServiceControllerStatus.Running);
            }

            controller.ExecuteCommand((int)CustomCommand.StartHostManager);
        }


    }
}
