using System;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ElasticBeanstalk.Models;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using log4net;


namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class RestartAppController : BaseConnectionContextCommand
    {
        static readonly ILog _logger = LogManager.GetLogger(typeof(RestartAppController));

        private readonly BeanstalkEnvironmentModel _beanstalkEnvironment;
        private readonly IAmazonElasticBeanstalk _beanstalk;

        public RestartAppController(BeanstalkEnvironmentModel beanstalkEnvironment,
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
                return ActionResults.CreateFailed(new ToolkitException("Unable to find Beanstalk application data",
                    ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            var msg =
                $"Are you sure you want to restart the application server(s) for the environment \"{_beanstalkEnvironment.Name}\"?\r\n\r\n" +
                "Note: Restarting the application server(s) may take several seconds.";

            if (!_toolkitContext.ToolkitHost.Confirm("Restart App", msg))
            {
                return ActionResults.CreateCancelled();
            }

            try
            {
                _logger.DebugFormat("Restarting app {0}", _beanstalkEnvironment.Id);
                _beanstalk.RestartAppServer(new RestartAppServerRequest() { EnvironmentId = _beanstalkEnvironment.Id });
                return new ActionResults().WithSuccess(true);
            }
            catch (Exception e)
            {
                _logger.Error($"Error Restarting Beanstalk Application: {_beanstalkEnvironment.Id}", e);
                _toolkitContext.ToolkitHost.ShowMessage("Error Restarting Beanstalk Application", $"Error restarting app server:{Environment.NewLine}{e.Message}");
                return ActionResults.CreateFailed(e);
            }
        }
    }
}
