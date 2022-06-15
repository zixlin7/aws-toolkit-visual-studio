using System;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.CloudWatch.Views;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Telemetry.Model;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Commands
{
    public class ViewLogStreamsCommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ViewLogStreamsCommand));
        private static readonly BaseMetricSource ViewLogGroupMetricSource = CloudWatchLogsMetricSource.LogGroupsView;

        public static ICommand Create(ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings)
        {
            return new RelayCommand(parameter => Execute(parameter, connectionSettings, toolkitContext));
        }

        private static void Execute(object parameter, AwsConnectionSettings connectionSettings,
            ToolkitContext toolkitContext)
        {
            var result = ViewLogStreams(parameter, connectionSettings, toolkitContext);
            RecordOpenLogGroup(result, connectionSettings, toolkitContext);
        }

        private static bool ViewLogStreams(object parameter, AwsConnectionSettings connectionSettings,
            ToolkitContext toolkitContext)
        {
            try 
            {
                if (!(parameter is LogGroup logGroup))
                {
                    throw new ArgumentException($"Parameter is not of expected type: {typeof(LogGroup)}");
                }

                var viewModel = CreateLogStreamsViewModel(toolkitContext, connectionSettings);
                viewModel.LogGroup = logGroup;

                var control = new LogStreamsViewerControl{ DataContext = viewModel };
                toolkitContext.ToolkitHost.OpenInEditor(control);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error viewing log streams", ex);
                toolkitContext.ToolkitHost.ShowError($"Error viewing log streams: {ex.Message}");

                return false;
            }
        }

        private static LogStreamsViewModel CreateLogStreamsViewModel(ToolkitContext toolkitContext,
            AwsConnectionSettings connectionSettings)
        {
            var repositoryFactory =
                toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)) as
                    IRepositoryFactory;
            if (repositoryFactory == null)
            {
                throw new Exception("Unable to load CloudWatch log streams data source");
            }

            var cwLogsRepository =
                repositoryFactory.CreateCloudWatchLogsRepository(connectionSettings);

            var viewModel = new LogStreamsViewModel(cwLogsRepository, toolkitContext);
            viewModel.RefreshCommand = RefreshLogsCommand.Create(viewModel);
            viewModel.ViewCommand = ViewLogEventsCommand.Create(toolkitContext, cwLogsRepository.ConnectionSettings);
            viewModel.ExportStreamCommand = ExportStreamCommand.Create(toolkitContext, cwLogsRepository);
            return viewModel;
        }

        private static void RecordOpenLogGroup(bool openResult,
            AwsConnectionSettings connectionSettings,
            ToolkitContext toolkitContext)
        {
            toolkitContext.TelemetryLogger.RecordCloudwatchlogsOpen(new CloudwatchlogsOpen()
            {
                AwsAccount = MetricsMetadata.AccountIdOrDefault(connectionSettings.GetAccountId(toolkitContext.ServiceClientManager)),
                AwsRegion = MetricsMetadata.RegionOrDefault(connectionSettings.Region),
                CloudWatchLogsPresentation = CloudWatchLogsPresentation.Ui,
                CloudWatchResourceType = CloudWatchResourceType.LogGroup,
                Result = openResult ? Result.Succeeded : Result.Failed,
                ServiceType = ViewLogGroupMetricSource.Service,
                Source = ViewLogGroupMetricSource.Location,
            });
        }
    }
}
