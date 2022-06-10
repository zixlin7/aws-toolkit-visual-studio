using System;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.CloudWatch.Views;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Telemetry.Model;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Commands
{
    /// <summary>
    /// Command that creates/displays a log groups explorer in a tool window
    /// </summary>
    public class ViewLogGroupsCommand : BaseConnectionContextCommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ViewLogGroupsCommand));
        private readonly BaseMetricSource _metricSource;

        public ViewLogGroupsCommand(BaseMetricSource metricSource,
            ToolkitContext context, AwsConnectionSettings connectionSettings)
            : base(context, connectionSettings)
        {
            _metricSource = metricSource;
        }

        public override ActionResults Execute()
        {
            ActionResults result = ViewLogGroups();

            EmitMetric(result);

            return result;
        }

        private ActionResults ViewLogGroups()
        {
            try
            {
                CreateLogGroupsViewer();
            }
            catch (Exception ex)
            {
                Logger.Error("Error viewing CloudWatch log groups", ex);
                _toolkitContext.ToolkitHost.OutputToHostConsole($"Unable to view CloudWatch log groups: {ex.Message}", true);
                return new ActionResults().WithSuccess(false);
            }

            return new ActionResults().WithSuccess(true);
        }

        private void CreateLogGroupsViewer()
        {
            var toolWindowFactory = _toolkitContext.ToolkitHost.GetToolWindowFactory();
            var logGroupsModel = CreateLogGroupsViewModel();
            var newControl = new LogGroupsViewerControl { DataContext = logGroupsModel };

            bool ConnectionSettingsFunc(BaseAWSControl awsControl)
            {
                var currentControl = awsControl as LogGroupsViewerControl;
                return currentControl?.ConnectionSettings?.CredentialIdentifier?.Id !=
                       newControl.ConnectionSettings?.CredentialIdentifier?.Id ||
                       currentControl?.ConnectionSettings?.Region?.Id != newControl.ConnectionSettings?.Region?.Id;
            }

            toolWindowFactory.ShowLogGroupsToolWindow(newControl, ConnectionSettingsFunc);
        }

        private LogGroupsViewModel CreateLogGroupsViewModel()
        {
            var repositoryFactory = _toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)) as IRepositoryFactory;
            if (repositoryFactory == null)
            {
                throw new Exception("Unable to load CloudWatch log groups data source");
            }

            var cwLogsRepository =
                repositoryFactory.CreateCloudWatchLogsRepository(ConnectionSettings);

            var viewModel = new LogGroupsViewModel(cwLogsRepository, _toolkitContext);
            viewModel.RefreshCommand = RefreshLogsCommand.Create(viewModel);
            viewModel.ViewCommand = ViewLogStreamsCommand.Create(_toolkitContext, cwLogsRepository.ConnectionSettings);
            viewModel.DeleteCommand = DeleteLogGroupCommand.Create(viewModel, _toolkitContext.ToolkitHost);
            return viewModel;
        }

        private void EmitMetric(ActionResults result)
        {
            Result metricsResult = result.Success ? Result.Succeeded : Result.Failed;

            _toolkitContext.TelemetryLogger.RecordCloudwatchlogsOpen(new CloudwatchlogsOpen()
            {
                AwsAccount = ConnectionSettings.GetAccountId(_toolkitContext.ServiceClientManager) ?? MetadataValue.NotSet,
                AwsRegion = ConnectionSettings.Region?.Id ?? MetadataValue.NotSet,
                CloudWatchLogsPresentation = CloudWatchLogsPresentation.Ui,
                CloudWatchResourceType = CloudWatchResourceType.LogGroupList,
                Result = metricsResult,
                ServiceType = _metricSource.Service,
                Source = _metricSource.Location,
            });
        }
    }
}
