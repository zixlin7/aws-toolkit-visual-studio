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
                return new ActionResults().WithSuccess(false);
            }

            string msg = string.Format(
                "Are you sure you want to restart the application server(s) for the environment \"{0}\"?\r\n\r\n" +
                "Note: Restarting the application server(s) may take several seconds."
                , _beanstalkEnvironment.Name);

            if (_toolkitContext.ToolkitHost.Confirm("Restart App", msg))
            {
                try
                {
                    _logger.DebugFormat("Restarting app {0}", _beanstalkEnvironment.Id);
                    _beanstalk.RestartAppServer(new RestartAppServerRequest() { EnvironmentId = _beanstalkEnvironment.Id });
                }
                catch (Exception e)
                {
                    _logger.Error($"Error Restarting app {_beanstalkEnvironment.Id}", e);
                    _toolkitContext.ToolkitHost.ShowMessage("Error Restarting", "Error restarting app server: " + e.Message);
                    return new ActionResults().WithSuccess(false);
                }
            }

            return new ActionResults().WithSuccess(true);
        }
    }
}
