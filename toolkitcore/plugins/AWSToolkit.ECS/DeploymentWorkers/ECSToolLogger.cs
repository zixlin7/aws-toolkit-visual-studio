using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.Common.DotNetCli.Tools;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class ECSToolLogger : IToolLogger
    {
        IDockerDeploymentHelper Helper { get; set; }

        internal ECSToolLogger(IDockerDeploymentHelper helper)
        {
            this.Helper = helper;
        }

        public void WriteLine(string message)
        {
            this.Helper.AppendUploadStatus(message);
        }
    }
}
