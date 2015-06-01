using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net;
using ThirdParty.Json.LitJson;

namespace AWSDeploymentHostManager.Tasks
{
    public class SystemInfoTask : Task
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(SystemInfoTask));

        private const string
            JSON_KEY_KEY = "key",
            JSON_KEY_IV = "iv";

        public override string Execute()
        {
            LOGGER.Info("Execute");

            StringBuilder sb = new StringBuilder();
            JsonWriter json = new JsonWriter(sb);

            json.WriteObjectStart();

            json.WritePropertyName(JSON_KEY_OP);
            json.Write(this.Operation);

            json.WritePropertyName(JSON_KEY_RESPONSE);
            json.Write(TASK_RESPONSE_OK);

            json.WritePropertyName(JSON_KEY_KEY);
            json.Write(HostManager.Config.Key);

            json.WritePropertyName(JSON_KEY_IV);
            json.Write(HostManager.Config.IV);

            json.WriteObjectEnd();
            return sb.ToString();
        }

        public override string Operation
        {
            get { return "SystemInfo"; }
        }
    }
}
