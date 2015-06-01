using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

using ThirdParty.Json.LitJson;
using AWSDeploymentHostManager.Persistence;

using log4net;

namespace AWSDeploymentHostManager.Tasks
{
    public class StatusTask : Task
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(StatusTask));

        private const string
            JSON_KEY_EVENTS = "events",
            JSON_KEY_PUBS = "publications",
            JSON_KEY_METRICS = "metrics",
            JSON_KEY_VERS = "versions",
            JSON_KEY_APP = "application",
            JSON_KEY_HOSTMANAGER = "hostmanager";

        private PersistenceManager pm = new PersistenceManager();

        public override string Execute()
        {
            StringBuilder sb = new StringBuilder();
            JsonWriter json = new JsonWriter(sb);

            json.WriteObjectStart();

            json.WritePropertyName(JSON_KEY_OP);
            json.Write(this.Operation);

            json.WritePropertyName(JSON_KEY_RESPONSE);
            json.Write(TASK_RESPONSE_OK);


            EmitVersion(json);
            EmitInstalledApplicationStats(json);


            json.WriteObjectEnd();
            return sb.ToString();
        }
        
        public override string Operation
        {
            get { return "Status"; }
        }

        private void EmitInstalledApplicationStats(JsonWriter json)
        {
            var configVersion = ConfigVersion.LoadLatestVersion();

            json.WritePropertyName("application");

            json.WriteObjectStart();

            json.WritePropertyName("is-app-installed");
            json.Write(configVersion.IsApplicationVersionInstalled);

            if (configVersion.IsApplicationVersionInstalled)
            {
                json.WritePropertyName("install-timestamp");
                json.Write(configVersion.ApplicationInstallTimestamp.ToUniversalTime().ToString("yyyy-MM-dd\\THH:mm:ss.fff\\Z"));

                ApplicationVersion appVersion = ApplicationVersion.LoadLatestVersion();
                if (appVersion != null)
                {
                    json.WritePropertyName("version-label");
                    json.Write(appVersion.VersionLabel);

                    json.WritePropertyName("s3-key");
                    json.Write(appVersion.S3Key);

                    json.WritePropertyName("s3-bucket");
                    json.Write(appVersion.S3Bucket);
                }
            }

            json.WriteObjectEnd();
        }

        private void EmitVersion(JsonWriter json)
        {
            json.WritePropertyName(JSON_KEY_VERS);
            json.WriteObjectStart();
            
            try
            {
                string hmVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                json.WritePropertyName(JSON_KEY_HOSTMANAGER);
                json.WriteObjectStart();
                json.WritePropertyName("version");
                json.Write(hmVersion);
                json.WriteObjectEnd();
            }
            catch
            {
                // Just omit the host manager version
            }

            json.WriteObjectEnd();
        }
    }
}
