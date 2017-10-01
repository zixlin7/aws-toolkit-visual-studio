using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class BaseWorker
    {
        protected ILog LOGGER = LogManager.GetLogger(typeof(BaseWorker));

        protected IDockerDeploymentHelper Helper { get; private set; }

        public BaseWorker(IDockerDeploymentHelper helper)
        {
            this.Helper = helper;
        }
    }
}
