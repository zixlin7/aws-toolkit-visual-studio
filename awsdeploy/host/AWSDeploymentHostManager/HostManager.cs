using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Threading;

using Microsoft.Web.Administration;

using ThirdParty.Json.LitJson;

using AWSDeploymentHostManager.Tasks;
using AWSDeploymentHostManager.Persistence;
using AWSDeploymentCryptoUtility;

using log4net;
using System.Diagnostics;

namespace AWSDeploymentHostManager
{
    public enum HostManagerStatus
    {
        Queue = 0,
        Running = 1,
        Stopping = 2
    }
    
    public class HostManager
    {
        public static string 
            LAST_HEALTHCHECK_TRANSITION = "LastHealthcheckTransition",
            LAST_HEALTHCHECK_CODE = "StatusCode";
        
        static public ILog LOGGER = LogManager.GetLogger(typeof(HostManager));

        private static HostManagerConfig config;
        private TaskFactory taskFactory;
        private CryptoUtil.EncryptionKeyTimestampIntegrator ec2MetaDataIntegrator;
        private static string appPath = null;
        private static string appName = null;
        private static string siteName = null;
        private static bool isSecure = false;
        private static int port = 80;
        private static HostManagerStatus status = HostManagerStatus.Queue;

        internal static HostManagerConfig Config
        {
            get
            {
                if (null == config)
                {
                    config = new HostManagerConfig("{}");
                }
                return config;
            }
            set { config = value; }
        }

        internal static string AppPath
        {
            get { return appPath; }
            set 
            { 
                appPath = value;
                string[] elements = value.Split(new char[] { '/' });

                SiteName = elements.First();
                if (elements.Length > 1)
                    AppName = String.Format("/{0}", elements.Last());
                else
                    AppName = "/";

                LOGGER.Info(String.Format("Setting SiteName to '{0}'", SiteName));
                LOGGER.Info(String.Format("Setting AppName to '{0}'", AppName));

                EntityObject persistItem;
                PersistenceManager pm = new PersistenceManager();
                IList<EntityObject> items = pm.SelectByStatus(EntityType.TimeStamp, "AppPath");
                if (items.Count > 0)
                {
                    DateTime latest = DateTime.MinValue;
                    persistItem = items[0];
                    foreach (EntityObject eo in items)
                    {
                        if ((persistItem.Status == "AppPath") && (eo.Timestamp > latest))
                        {
                            persistItem = eo;
                            latest = eo.Timestamp;
                        }
                    }
                }
                else
                {
                    persistItem = new EntityObject(EntityType.TimeStamp);
                    persistItem.Status = "AppPath";
                }
                persistItem.Parameters["AppPath"] = value;
                persistItem.Parameters["AppName"] = AppName;
                persistItem.Parameters["SiteName"] = SiteName;

                pm.Persist(persistItem);
            }
        }
        internal static string AppName
        {
            get { return appName; }
            set { appName = value; }
        }
        internal static string SiteName
        {
            get { return siteName; }
            set { siteName = value; }
        }
        internal static bool IsSecure
        {
            get { return isSecure; }
            set { isSecure = value; }
        }        
        internal static int Port
        {
            get { return port; }
            set { port = value; }
        }
        internal HostManagerStatus Status
        {
            get { return status; }
            set { status = value; }
        }

        internal static HostManager instance = null;
        internal static int SyncTasksRunning = 0;
        internal static int ASyncTasksRunning = 0;

        public HostManager(string configuration)
        {
            try
            {
                CheckForNewVersionAndUpdate();
                Initialize(configuration);
            }
            catch (Exception e)
            {
                LOGGER.Error("Unexpected Exception: ", e);
                throw;
            }
        }

        private void Initialize(string configuration)
        {
            Event.LogMilestone("host_manager", "Starting Host Manager", "host_manager");

            instance = this;
            if (string.IsNullOrEmpty(configuration))
                config = HostManagerConfig.CreateFromUserData();
            else
                config = new HostManagerConfig(configuration);

            ec2MetaDataIntegrator = delegate(string ts)
            {
                // Validate timestamp to stop replay attacks
                DateTime time;
                string[] validTimestampFormats = { "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ" };
                if (DateTime.TryParseExact(ts, validTimestampFormats, CultureInfo.InvariantCulture , DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out time))
                {
                     time = time.AddMinutes(10);
                     if (DateTime.UtcNow > time)
                     {
                         LOGGER.Warn(String.Format("Task request timestamp too far in the past [{0} > {1}]", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), ts));
                         HostManager.SynchronizeTime();
                         return String.Empty;
                     }
                     time = time.AddMinutes(-20);
                     if (DateTime.UtcNow < time)
                     {
                         LOGGER.Warn(String.Format("Task request timestamp too far in the future [{0} < {1}]", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), ts));
                         HostManager.SynchronizeTime();
                         return String.Empty;
                     }
                }
                else
                {
                    LOGGER.Error(String.Format("Failed to parse task request timestamp. [{0}]", ts));
                    return String.Empty;
                }

                return string.Format("{0}{1}{2}", config.Ec2InstanceId, config.Ec2ReservationId, ts);
            };

            //
            // Set up the task factory
            //

            taskFactory = new TaskFactory();

            taskFactory.RegisterTask("Status",              typeof(StatusTask));
            taskFactory.RegisterTask("RestartAppServer",    typeof(RestartAppServerTask));
            taskFactory.RegisterTask("UpdateAppVersion",    typeof(UpdateAppVersionTask));
            taskFactory.RegisterTask("UpdateConfiguration", typeof(UpdateConfigurationTask));
            taskFactory.RegisterTask("SendFileToS3",        typeof(SendFileToS3Task));
            taskFactory.RegisterTask("Tail",                typeof(TailTask));
            taskFactory.RegisterTask("SystemInfo",          typeof(SystemInfoTask));
            taskFactory.RegisterTask("Events", typeof(EventsTask));

            //taskFactory.RegisterTask("SelfUpdate",          typeof(SelfUpdateTask));
            //taskFactory.RegisterTask("Unmanage",            typeof(UnmanageTask));


            //
            // Prime the log directory scanner
            //

            SimpleAddIfNotPresent("LogDirectoryScan", "name", "IISLogs", "path", config.ASPLogLocation);
            SimpleAddIfNotPresent("LogDirectoryScan", "name", "Deployment", "path", "..\\logs\\Deployment");
            SimpleAddIfNotPresent("LogDirectoryScan", "name", "HostManager", "path", "..\\logs\\HostManager");
            SimpleAddIfNotPresent("LogDirectoryScan", "name", "HostManagerListener", "path", "..\\logs\\HostManagerApp");
            SimpleAddIfNotPresent("LogDirectoryScan", "name", "MagicHarp", "path", "..\\logs\\MagicService");
            SimpleAddIfNotPresent("EventLog", "name", "Application", "number", "-1");

            Event.LogMilestone("host_manager", "Host Manager startup complete", "host_manager");

            if (ApplicationVersion.LoadLatestVersion() == null)
            {
                UpdateApplicationVersion();
            }
            else
            {
                EntityObject storedItem = null;
                PersistenceManager pm = new PersistenceManager();
 
                IList<EntityObject> items = pm.SelectByStatus(EntityType.TimeStamp, "AppPath");
                if (items.Count > 0)
                {
                    DateTime latest = DateTime.MinValue;
                    storedItem = items[0];
                    foreach (EntityObject eo in items)
                    {
                        if ((storedItem.Status == "AppPath") && (eo.Timestamp > latest))
                        {
                            storedItem = eo;
                            latest = eo.Timestamp;
                        }
                    }
                    appPath = storedItem.Parameters["AppPath"];
                    AppName = storedItem.Parameters["AppName"];
                    SiteName = storedItem.Parameters["SiteName"];

                    ConfigHelper.SetBindings();
                }
                else
                {
                    LOGGER.Warn("Application already deployed but could not find persisted AppPath information.");
                    Event.LogWarn("HostManager","Application already deployed but could not find persisted AppPath information.");
                }
            }

            status = HostManagerStatus.Running;
        }

        private void SimpleAddIfNotPresent(string status, string req_Key, string req_Value, string add_Key, string add_Value)
        {
            Dictionary<string, string> find = new Dictionary<string, string>();
            find.Add(req_Key, req_Value);

            Dictionary<string, string> additional = new Dictionary<string, string>();
            additional.Add(add_Key, add_Value);

            AddIfNotPresent(status, find, additional);
        }
        private void AddIfNotPresent(string status, Dictionary<string, string> find, Dictionary<string, string> additional)
        {
            PersistenceManager pm = new PersistenceManager();

            IEnumerable<EntityObject> timeStamps = pm.SelectByStatus(EntityType.TimeStamp, status);

            foreach (KeyValuePair<string, string> kvp in find)
            {
                timeStamps = timeStamps.Where(eo => String.Equals(eo.Parameters[kvp.Key],kvp.Value,StringComparison.InvariantCultureIgnoreCase));
            }

            if (!timeStamps.Any())
            {
                EntityObject ts = new EntityObject(EntityType.TimeStamp);
                ts.Status = status;
                foreach (KeyValuePair<string, string> kvp in find)
                {
                    ts.Parameters[kvp.Key] = kvp.Value;
                }
                foreach (KeyValuePair<string, string> kvp in additional)
                {
                    ts.Parameters[kvp.Key] = kvp.Value;
                }
                pm.Persist(ts);
            }
        }

        internal void Exit()
        {
            while (SyncTasksRunning != 0)
            {
                LOGGER.DebugFormat("{0} of sync task still running, sleeping for 1 second.", SyncTasksRunning);
                Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
            }

            while (ASyncTasksRunning != 0)
            {
                LOGGER.DebugFormat("{0} of async task still running, sleeping for 1 second.", SyncTasksRunning);
                Thread.Sleep((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
            }
        }

        internal string ProcessTaskRequest(string request)
        {
            string response = String.Empty;

            byte[] iv;
            string taskDescriptor = null;
            Task task;

            try
            {
                LOGGER.DebugFormat("Starting decrypt: {0}", request);
                JsonData requestJson;
                try
                {
                     requestJson = CryptoUtil.DecryptRequest(request, ec2MetaDataIntegrator);
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    if (!Config.VerifyCryptoValues())
                    {
                        throw;
                    }
                    else
                    {
                        requestJson = CryptoUtil.DecryptRequest(request, ec2MetaDataIntegrator);
                    }
                } 
                LOGGER.DebugFormat("Finished decrypt: {0}", requestJson.ToJson());

                iv = Convert.FromBase64String((string)requestJson[CryptoUtil.JSON_KEY_IV]);
                taskDescriptor = (string)requestJson[CryptoUtil.JSON_KEY_PAYLOAD];

                task = taskFactory.CreateTaskFromRequest(taskDescriptor);
                LOGGER.DebugFormat("Created task {0}", task.Operation);

                string taskResponse = task.Execute();
                LOGGER.DebugFormat("Finished Task {0} with response {1}", task.Operation, taskResponse);

                if (task is UpdateConfigurationTask && ((UpdateConfigurationTask)task).NewConfiguration != null)
                {
                    UpdateConfig(((UpdateConfigurationTask)task).NewConfiguration);
                }

                response = CryptoUtil.EncryptResponse(taskResponse, iv, CryptoUtil.Timestamp(), ec2MetaDataIntegrator);
            }
            catch (ArgumentException ae)
            {
                LOGGER.Error(string.Format("Badly Formatted Request: {0}", taskDescriptor), ae);
            }
            catch (JsonException je)
            {
                LOGGER.Error("Badly Formatted Request.", je);
            }
            catch (Exception e)
            {
                LOGGER.Error("Unknown error processing request", e);
            }

            return response;
        }

        internal string GetCurrentVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        internal string PerformApplicationHealthcheck()
        {
            string url = null;
            try
            {
                url = String.Format("http{0}://localhost:{1}/", isSecure ? "s" : "", port) + config.ApplicationHealthcheckUrl;
                var httpRequest = WebRequest.Create(url) as HttpWebRequest;
                httpRequest.Method = "HEAD";
                httpRequest.UserAgent = "AWSHealthCheck";

                using (var httpResponse = httpRequest.GetResponse() as HttpWebResponse)
                {
                    var last = LastHealthcheckTransition;
                    bool start = last.Parameters[LAST_HEALTHCHECK_CODE] == "Start";

                    if (last.Parameters[LAST_HEALTHCHECK_CODE] != httpResponse.StatusCode.ToString())
                    {
                        last.Parameters[LAST_HEALTHCHECK_CODE] = httpResponse.StatusCode.ToString();
                        new PersistenceManager().Persist(last);
                    }

                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        LOGGER.DebugFormat("Passed health check to {0}", url);
                        
                        if (start)
                            Event.LogMilestone("healthcheck", "First successful healthcheck since deployment", "healthcheck");

                        return ((int)HttpStatusCode.OK).ToString();
                    }
                    else
                    {
                        LOGGER.DebugFormat("Failed health check to {0} with status code {1}", url, httpResponse.StatusCode);
                        return ((int)httpResponse.StatusCode).ToString();
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Debug(string.Format("Failed health check to {0}", url), e);
                return "-1";
            }
        }

        public static EntityObject LastHealthcheckTransition
        {
            get
            {
                var pm = new PersistenceManager();
                var eos = pm.SelectByStatus(EntityType.TimeStamp, LAST_HEALTHCHECK_TRANSITION);
                EntityObject ts;
                if (null == eos || eos.Count < 1)
                {
                    ts = new EntityObject(EntityType.TimeStamp);
                    ts.Status = LAST_HEALTHCHECK_TRANSITION;
                    ts.Parameters[LAST_HEALTHCHECK_CODE] = "Start";
                    pm.Persist(ts);
                }
                else
                    ts = eos[0];

                return ts;
            }
        }

        public void CheckForNewVersionAndUpdate()
        {
            try
            {
                var selfUpdate = new SelfUpdateTask();
                selfUpdate.CheckForNewVersionAndUpdate();
            }
            catch (Exception e)
            {
                LOGGER.Error("Unexpected Exception: ", e);
            }
        }

        internal void UpdateConfig()
        {
            var task = new UpdateConfigurationTask();
            task.Execute();
            if (task.NewConfiguration != null)
                UpdateConfig(task.NewConfiguration);
            else
            {
                var version = ConfigVersion.LoadLatestVersion();
                if (!version.IsApplicationVersionInstalled)
                    UpdateApplicationVersion();
            }
        }

        private void UpdateConfig(HostManagerConfig newConfig)
        {
            LOGGER.Info("Applying configuration update.");
            try
            {
                config = newConfig;
                ConfigHelper.ConfigAppPool();
                ConfigHelper.ConfigEnvironmentVariables();
                ConfigHelper.SetLocalEnvironmentVariables();
                UpdateApplicationVersion();
            }
            catch (Exception e)
            {
                LOGGER.Warn(String.Format("Error setting up configuration, check application pool settings and envirnoment properties may not be available.\n {0}",e));
                Event.LogInfo("HostManager", String.Format("Error setting up configuration, check application pool settings and envirnoment properties may not be available.\n {0}", e));
            }
        }

        private void UpdateApplicationVersion()
        {
            var task = new UpdateAppVersionTask();
            task.Execute();
        }

        internal static void SynchronizeTime()
        {
            string w32tmExe = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\w32tm.exe");

            if (w32tmExe != null)
            {
                try
                {
                    Process w32tm = new Process();
                    w32tm.StartInfo.FileName = w32tmExe;
                    w32tm.StartInfo.Arguments = @"/resync";

                    DateTime before = DateTime.UtcNow;

                    w32tm.Start();
                    w32tm.WaitForExit(3000);

                    DateTime after = DateTime.UtcNow;

                    TimeSpan correction = after - before;

                    LOGGER.Info(String.Format("Corrected {0} seconds of clock skew.", correction.TotalSeconds));
                }
                catch (Exception e)
                {
                    LOGGER.Warn("Error while trying to correct clock skew", e);
                }
            }

            Metric.LogCountMetric("IIS.ClockSkewCorrections", "1");
        }
    }
}
