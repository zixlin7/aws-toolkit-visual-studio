using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Web;
using Microsoft.Web.Administration;
using Microsoft.Web.Deployment;

using System.Threading;

using ThirdParty.Json.LitJson;
using AWSDeploymentHostManager.Persistence;

using log4net;

namespace AWSDeploymentHostManager.Tasks
{
    public class UpdateAppVersionTask : Task
    {
        static readonly IDictionary<string, string> FINAL_EVENT_PARAMETER;
        static ILog LOGGER = LogManager.GetLogger(typeof(UpdateAppVersionTask));
        static ILog DEPLOYMENT_LOGGER = LogManager.GetLogger("DeploymentLog");

        string _tempFileLocation = Path.GetTempFileName() + ".zip";
        string _s3Url;
        string _s3HeadUrl;
        string _s3Bucket;
        string _s3Key;
        string _digest;
        ApplicationVersion _version;

        static UpdateAppVersionTask()
        {
            FINAL_EVENT_PARAMETER = new Dictionary<string, string>();
            FINAL_EVENT_PARAMETER["FinalEvent"] = "true";
        }

        public override string Operation
        {
            get { return "UpdateAppVersion"; }
        }

        public string S3Url
        {
            get { return this._s3Url; }
            set { this._s3Url = value; }
        }

        public string S3HeadUrl
        {
            get { return this._s3HeadUrl; }
            set { this._s3HeadUrl = value; }
        }

        public string S3Bucket
        {
            get { return _s3Bucket; }
            set { _s3Bucket = value; }
        }

        public string S3Key
        {
            get { return _s3Key; }
            set { _s3Key = value; }
        }

        public ApplicationVersion ApplicationVersion
        {
            get { return this._version; }
        }

        public override string Execute()
        {
            Interlocked.Increment(ref HostManager.ASyncTasksRunning);
            this._s3Url = HostManager.Config.ApplicationFullURL; 
            this._s3HeadUrl = HostManager.Config.ApplicationHeadURL;
            this._digest = HostManager.Config.ApplicationDigest;

            ThreadPool.QueueUserWorkItem(this.DoExecute);
            return GenerateResponse(TASK_RESPONSE_DEFER);
        }

        public void DoExecute(object state)
        {
            // Reset the successful healthcheck timestamp and emit a milestone
            try
            {
                LOGGER.InfoFormat("Called UpdateAppVersion url: {0}, headurl: {1}, digest: {2}", _s3Url, _s3HeadUrl, _digest);
                ApplicationVersion currentVersion = ApplicationVersion.LoadLatestVersion();

                if (currentVersion != null)
                {
                    DateTime lastUpdated = S3Util.GetContentLastUpdated(this._s3HeadUrl, Amazon.S3.HttpVerb.HEAD);

                    if (this._s3Url.Equals(currentVersion.Url) && lastUpdated < currentVersion.Timestamp)
                    {
                        LOGGER.Info("Application data is the same. No Update needed.");
                        return;
                    }
                }

                var last = HostManager.LastHealthcheckTransition;
                last.Parameters[HostManager.LAST_HEALTHCHECK_CODE] = "Start";
                new PersistenceManager().Persist(last);

                Event.LogMilestone(Operation, "Started Application Update", "deployment");

                try
                {
                    this._version = new ApplicationVersion(this._s3Url);
                    if (this._digest != null && this._digest.Length > 0)
                    {
                        this._version.Digest = this._digest;
                    }

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    S3Util.WriteToFile(this._s3Url, this._tempFileLocation);
                    stopwatch.Stop();

                    TimeSpan timeToDownload = stopwatch.Elapsed;
                    long fileSize = (new FileInfo(this._tempFileLocation).Length);
                    double downloadRate = (double)fileSize / timeToDownload.TotalSeconds; // bytes per second

                    Metric.LogTimeMetric("IIS.AppDownloadTime", timeToDownload.TotalMilliseconds.ToString());
                    Metric.LogCountMetric("IIS.AppDownloadRate", (downloadRate / 1000.0).ToString());

                    if (null == this._digest || S3Util.VerifyFileDigest(this._tempFileLocation, this._digest))
                    {
                        // MSBuild package bug work around.
                        FixManagedRuntime(this._tempFileLocation);

                        if (deployPackage())
                        {
                            ConfigHelper.ConfigAppPool();
                            ConfigHelper.SetBindings();
                        }
                        else
                        {
                            LOGGER.Error(string.Format("Deploy failed, not iisApp. (S3Url: {0}, Bucket: {1}, Key: {2}, Version: {3})",
                              this._s3Url, this._version.S3Bucket, this._version.S3Key, this._version.S3Version));
                            Event.LogEvent(Operation, String.Format("Deploy failed, not iisApp"), Event.EVENT_SEVERITY_WARN, FINAL_EVENT_PARAMETER);
                            return;
                        }
                    }
                    else
                    {
                        Event.LogEvent(Operation, "Digest mismatch failure", Event.EVENT_SEVERITY_WARN, FINAL_EVENT_PARAMETER);
                        LOGGER.Warn("Digest Mismatch failure");
                        return;
                    }
                }
                catch (Exception e)
                {
                    LOGGER.Error(string.Format("Exception deploy release. (S3Url: {0}, Bucket: {1}, Key: {2}, Version: {3})",
                        this._s3Url, this._version.S3Bucket, this._version.S3Key, this._version.S3Version), e);
                    Event.LogEvent(Operation, String.Format("Deploy failed with exception: {0}", e.ToString()), Event.EVENT_SEVERITY_WARN, FINAL_EVENT_PARAMETER);
                    return;
                }

                LOGGER.InfoFormat("UpdateAppVersion Completed. (S3Url: {0}, Bucket: {1}, Key: {2}, Version: {3})",
                        this._s3Url, this._version.S3Bucket, this._version.S3Key, this._version.S3Version);
                Event.LogEvent(Operation, "UpdateAppVersion Completed", Event.EVENT_SEVERITY_INFO, FINAL_EVENT_PARAMETER);

                this._version.Persist();

                var configVersion =  ConfigVersion.LoadLatestVersion();
                configVersion.MarkApplicationVersionInstalled();
                configVersion.Persist();

                publishWaitCondition("SUCCESS", "");

                return;
            }
            catch (Exception e)
            {
                LOGGER.Error(string.Format("Unexpected Exception: {0}", e));
                Event.LogEvent(Operation, String.Format("Deploy failed with exception: {0}", e.ToString()), Event.EVENT_SEVERITY_WARN, FINAL_EVENT_PARAMETER);
                return;
            }
            finally
            {
                Interlocked.Decrement(ref HostManager.ASyncTasksRunning);
            }
        }

        void publishWaitCondition(string status, string message) 
        {
            if (string.IsNullOrEmpty(HostManagerConfig.WaitConditionSignalURL))
                return;

            try
            {
                Amazon.CloudFormation.Util.AmazonCloudFormationUtil.SignalWaitCondition(HostManagerConfig.WaitConditionSignalURL, status, "Appliation Deployment", Guid.NewGuid().ToString(), message);
            }
            catch (Exception e) 
            {
                LOGGER.Error("Failed to signal wait condition", e);
                Event.LogEvent(Operation, "Failed to signal wait condition", Event.EVENT_SEVERITY_INFO);
            }
        }

        private bool deployPackage()
        {
            DeploymentBaseOptions srcBaseOptions = new DeploymentBaseOptions();
            srcBaseOptions.Trace += TraceEventHandler;
            srcBaseOptions.TraceLevel = TraceLevel.Verbose;
            srcBaseOptions.IncludeAcls = true;

            using (DeploymentObject depObj = DeploymentManager.CreateObject("package", this._tempFileLocation, srcBaseOptions))
            {
                DeploymentSyncOptions destSyncOptions = new DeploymentSyncOptions();

                DeploymentBaseOptions destBaseOptions = new DeploymentBaseOptions();
                destBaseOptions.Trace += TraceEventHandler;
                destBaseOptions.TraceLevel = TraceLevel.Info;

                string appPath = null;
                foreach (DeploymentSyncParameter param in depObj.SyncParameters)
                {
                    if (param.WellKnownTags == DeploymentWellKnownTag.IisApp)
                    {
                        appPath = param.Value;
                        HostManager.AppPath = appPath;
                        break;
                    }
                }

                if (null == appPath)
                {
                    LOGGER.Warn("Deployment package did not contain an iisApp");
                    Event.LogWarn(Operation, "Deployment package did not contain an iisApp");
                    return false;
                }

                var metadata = HostManager.Config.LoadResourceMetaData();
                ConfigHelper.ConfigAppPool();
                ConfigHelper.ConfigEnvironmentVariables();

                try
                {
                    depObj.SyncTo("auto", "", destBaseOptions, destSyncOptions);
                }
                finally
                {
                    FileInfo deployLogFile = new FileInfo("..\\logs\\Deployment\\Deployment.log");
                    FileInfo publishLogFile = deployLogFile.CopyTo(String.Format("..\\logs\\Deployment\\Deployment.{0}.log", DateTime.UtcNow.ToString("yyyy-MM-dd\\THH_mm_ss_fffZ")));
                    publishLogFile.LastWriteTime = DateTime.Now;
                    log4net.Config.XmlConfigurator.Configure();
                }
                ConfigHelper.SetLocalEnvironmentVariables();
                ConfigHelper.ConfigConnectionStrings(metadata);

                ConfigHelper.AdjustSiteWebConfig(appPath);
                ConfigHelper.SetRootAppPool();
            }
            return true;
        }

        void TraceEventHandler(object sender, DeploymentTraceEventArgs traceEvent)
        {
            switch (traceEvent.EventLevel)
            {
                case TraceLevel.Error:
                    Event.LogCritical(this.Operation, traceEvent.Message);
                    LOGGER.Error(traceEvent.Message);
                    DEPLOYMENT_LOGGER.Error(traceEvent.Message);
                    break;
                case TraceLevel.Warning:
                    Event.LogWarn(this.Operation, traceEvent.Message);
                    LOGGER.Warn(traceEvent.Message);
                    DEPLOYMENT_LOGGER.Warn(traceEvent.Message);
                    break;
                case TraceLevel.Info:
                    LOGGER.Info(traceEvent.Message);
                    DEPLOYMENT_LOGGER.Info(traceEvent.Message);
                    break;
                case TraceLevel.Verbose:
                    LOGGER.Debug(traceEvent.Message);
                    DEPLOYMENT_LOGGER.Debug(traceEvent.Message);
                    break;
            }
        }

        /// <summary>
        /// This method is to work around a bug in MSBuild when VS2010 and VS2012 are installed on the client machine and msbuild is used to 
        /// create the archive.  It will set the managed runtime version in the archive.xml to version 4.5 which doesn't exist since 4.5 is just 
        /// the framework version.
        /// </summary>
        /// <param name="archivePath">Path to the archive to fix up</param>
        static void FixManagedRuntime(string archivePath)
        {
            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Update))
            {
                ZipArchiveEntry entry = archive.GetEntry("archive.xml");
                if (entry == null)
                    return;

                string originalContent = null;
                using (StreamReader reader = new StreamReader(entry.Open()))
                {
                    originalContent = reader.ReadToEnd();
                }

                var fixedContent = originalContent.Replace("managedRuntimeVersion=\"v4.5\"", "managedRuntimeVersion=\"v4.0\"");

                if (!string.Equals(originalContent, fixedContent))
                {
                    LOGGER.InfoFormat("Working around MSBuild bug setting managedRuntimeVersion to v4.5");

                    using (Stream stream = entry.Open())
                    {
                        var buffer = System.Text.Encoding.UTF8.GetBytes(fixedContent);
                        stream.Write(buffer, 0, buffer.Length);
                        stream.SetLength(buffer.Length);
                    }
                }
            }
        }
    }
}
