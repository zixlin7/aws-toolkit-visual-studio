using System;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ElasticBeanstalk.Models;
using Amazon.AWSToolkit.Navigator;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class RebuildEnvironmentController : BaseConnectionContextCommand
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(RebuildEnvironmentController));

        private readonly BeanstalkEnvironmentModel _beanstalkEnvironment;
        private readonly AmazonElasticBeanstalkClient _beanstalk;

        public RebuildEnvironmentController(BeanstalkEnvironmentModel beanstalkEnvironment,
            ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings)
            : base(toolkitContext, connectionSettings)
        {
            _beanstalkEnvironment = beanstalkEnvironment;
            _beanstalk = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonElasticBeanstalkClient>(ConnectionSettings.CredentialIdentifier, ConnectionSettings.Region);
        }

        public override ActionResults Execute()
        {
            if (_beanstalkEnvironment == null)
            {
                return new ActionResults().WithSuccess(false);
            }

            string msg = string.Format(
                "Are you sure you want to rebuild the environment \"{0}\"?\r\n\r\n" +
                "Note: Rebuilding the environment may take " +
                "several minutes during which your application " +
                "will not be available. Use \"Restart App Servers\" " + 
                "if you only need to restart the application."
                , _beanstalkEnvironment.Name);
            if (_toolkitContext.ToolkitHost.Confirm("Rebuild Environment", msg))
            {
                try
                {
                    _logger.DebugFormat("Rebuilding environment {0}", _beanstalkEnvironment.Id);
                    _beanstalk.RebuildEnvironment(new RebuildEnvironmentRequest() { EnvironmentId = _beanstalkEnvironment.Id });
                }
                catch (Exception e)
                {
                    _logger.Error(string.Format("Error rebuilding environment {0}", _beanstalkEnvironment.Id), e);
                    _toolkitContext.ToolkitHost.ShowMessage("Error Rebuilding", "Error rebuilding environment: " + e.Message);
                    return new ActionResults().WithSuccess(false);
                }
            }

            return new ActionResults().WithSuccess(true);
        }

    }
}
