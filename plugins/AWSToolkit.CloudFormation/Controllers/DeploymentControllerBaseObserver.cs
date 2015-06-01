using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AWSDeployment;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    internal class DeploymentControllerBaseObserver : DeploymentObserver
    {
        ILog _logger;

        public DeploymentControllerBaseObserver(ILog logger)
        {
            _logger = logger;
        }

        protected void WriteOutputMessage(string message)
        {
            ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(message, true);
            ToolkitFactory.Instance.ShellProvider.UpdateStatus(message);
            _logger.InfoFormat("Publish to AWS CloudFormation: {0}", message);
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
            _logger.Warn(msg);
        }
    }
}
