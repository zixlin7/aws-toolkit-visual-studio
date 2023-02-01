using System;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;
using Amazon.AWSToolkit.ElasticBeanstalk.Utils;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Telemetry;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class DeleteApplicationController : BaseDeleteApplicationController, IContextCommand
    {
        private readonly ToolkitContext _toolkitContext;

        public DeleteApplicationController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public ActionResults Execute(IViewModel model)
        {
            ActionResults actionResults = null;

            void Invoke() => actionResults = DeleteApplication(model);

            void Record(ITelemetryLogger _)
            {
                var viewModel = model as ApplicationViewModel;
                var awsConnectionSettings = viewModel?.ElasticBeanstalkRootViewModel?.AwsConnectionSettings;
                _toolkitContext.RecordBeanstalkDeleteApplication(actionResults, awsConnectionSettings);
            }

            _toolkitContext.TelemetryLogger.InvokeAndRecord(Invoke, Record);
            return actionResults;
        }

        private ActionResults DeleteApplication(IViewModel model)
        {
            var appModel = model as ApplicationViewModel;
            if (appModel == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find Beanstalk application data",
                    ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            var client = appModel.BeanstalkClient;
            var applicationName = appModel.Application.ApplicationName;
            return Execute(client, applicationName);
        }

        private ActionResults Execute(IAmazonElasticBeanstalk client, string applicationName)
        {
            var msg = $"Are you sure you want to delete application {applicationName}?  " +
                      "This will terminate any environments and versions associated with this application. " +
                      "It also deletes any Amazon RDS DB Instances created with the environment(s). To save your data, " +
                      "create a snapshot before you delete your application.";


            if (!_toolkitContext.ToolkitHost.Confirm("Delete Beanstalk Application", msg))
            {
                return ActionResults.CreateCancelled();
            }

            try
            {
                var descRequest = new DescribeEnvironmentsRequest() { ApplicationName = applicationName };
                base.TerminateEnvironments(client, descRequest);
                client.DeleteApplication(new DeleteApplicationRequest() { ApplicationName = applicationName });
                return new ActionResults().WithSuccess(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError("Delete Beanstalk Application Error",
                    $"Error deleting application {applicationName}:{Environment.NewLine}{e.Message}");
                return ActionResults.CreateFailed(e);
            }
        }
    }
}
