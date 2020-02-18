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

        public void WriteLine(string message, params object[] args)
        {
            this.Helper.AppendUploadStatus(string.Format(message, args));
        }
    }
}
