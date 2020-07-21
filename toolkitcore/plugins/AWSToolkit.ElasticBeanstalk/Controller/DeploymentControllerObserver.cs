using Amazon.AWSToolkit.Shared;
using AWSDeployment;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class DeploymentControllerObserver : DeploymentObserver
    {
        private readonly ILog _logger;
        private readonly IAWSToolkitShellProvider _shellProvider;

        public DeploymentControllerObserver(ILog logger)
            : this(logger, ToolkitFactory.Instance.ShellProvider)
        {
        }

        public DeploymentControllerObserver(ILog logger, IAWSToolkitShellProvider shellProvider)
        {
            _logger = logger;
            _shellProvider = shellProvider;
        }

        protected void WriteOutputMessage(string message)
        {
            _shellProvider.OutputToHostConsole(message, true);
            _shellProvider.UpdateStatus(message);
            _logger.InfoFormat("Publish to AWS Elastic Beanstalk: {0}", message);
        }

        public override void Status(string messageFormat, params object[] list)
        {
            WriteOutputMessage(string.Format(messageFormat, list));
        }

        public override void Progress(string messageFormat, params object[] list)
        {
            WriteOutputMessage(string.Format(messageFormat, list));
        }

        public override void Info(string messageFormat, params object[] list)
        {
            _logger.Info(string.Format(messageFormat, list));
        }

        public override void Warn(string messageFormat, params object[] list)
        {
            string msg = string.Format(messageFormat, list);
            WriteOutputMessage(msg);
            _logger.Warn(msg);
        }

        public override void Error(string messageFormat, params object[] list)
        {
            string msg = string.Format(messageFormat, list);
            WriteOutputMessage(msg);
            _logger.Error(msg);
        }
    }
}
