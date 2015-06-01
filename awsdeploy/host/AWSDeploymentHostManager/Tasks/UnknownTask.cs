using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ThirdParty.Json.LitJson;

using log4net;

namespace AWSDeploymentHostManager.Tasks
{
    class UnknownTask : Task
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(UnknownTask));
        string taskName = "Unknown";

        public UnknownTask(string taskName)
        {
            this.taskName = taskName;
        }

        public override string Execute()
        {
            JsonData response = new JsonData();

            if (parameters.ContainsKey("unknownOpKey"))
            {
                response["operation"] = parameters["unknownOpKey"];
            }

            LOGGER.WarnFormat("Unknown task '{0}' requested", response["operation"] ?? Operation);

            response["response"] = TASK_RESPONSE_UNKNOWN;

            return GenerateResponse(response);
        }
        
        public override string Operation
        {
            get { return taskName; }
        }
    }
}
