using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net;

namespace AWSDeploymentHostManager.Tasks
{
    public class SystemUpdateTask : Task
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(SystemUpdateTask));

        public override string Execute()
        {
            LOGGER.Info("Execute");
            return GenerateResponse(TASK_RESPONSE_DEFER);

        }

        public override string Operation
        {
            get { return "SystemUpdate"; }
        }
    }
}
