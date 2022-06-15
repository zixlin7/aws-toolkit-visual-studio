using System;
using System.Linq;
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
    public class ViewLogEventsCommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ViewLogEventsCommand));
        private static readonly BaseMetricSource ViewLogStreamMetricSource = CloudWatchLogsMetricSource.LogGroupView;

        public static ICommand Create(ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings)
        {
            return new RelayCommand(parameter => Execute(parameter, connectionSettings, toolkitContext));
        }

        private static void Execute(object parameter,
            AwsConnectionSettings connectionSettings, ToolkitContext toolkitContext)
        {
            var result = ViewLogEvents(parameter, connectionSettings, toolkitContext);
            RecordOpenLogGroup(result, connectionSettings, toolkitContext);
        }

        private static bool ViewLogEvents(object parameter,
            AwsConnectionSettings connectionSettings, ToolkitContext toolkitContext)
        {
            try
            {
                var parameters = (object[]) parameter;
                if (parameters == null || parameters.Count() != 2)
                {
                    throw new ArgumentException($"Expected parameters: 2, Found: {parameters?.Count()}");
                }

                if (parameters.Any(x => x.GetType() != typeof(string)))
                {
                    throw new ArgumentException($"Parameters are not of expected type: {typeof(string)}");
                }

                var viewModel = CreateLogEventsViewModel(toolkitContext, connectionSettings);
                viewModel.LogGroup = parameters[0] as string;
                viewModel.LogStream = parameters[1] as string;

                var control = new LogEventsViewerControl { DataContext = viewModel };
                toolkitContext.ToolkitHost.OpenInEditor(control);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error viewing log events", ex);
                toolkitContext.ToolkitHost.ShowError($"Error viewing log events: {ex.Message}");

                return false;
            }
        }

        private static LogEventsViewModel CreateLogEventsViewModel(ToolkitContext toolkitContext,
            AwsConnectionSettings connectionSettings)
        {
            var repositoryFactory =
                toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)) as
                    IRepositoryFactory;
            if (repositoryFactory == null)
            {
                throw new Exception("Unable to load CloudWatch log events data source");
            }

            var cwLogsRepository =
                repositoryFactory.CreateCloudWatchLogsRepository(connectionSettings);

            var viewModel = new LogEventsViewModel(cwLogsRepository, toolkitContext);
            viewModel.RefreshCommand = RefreshLogsCommand.Create(viewModel);
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
                CloudWatchResourceType = CloudWatchResourceType.LogStream,
                Result = openResult ? Result.Succeeded : Result.Failed,
                ServiceType = ViewLogStreamMetricSource.Service,
                Source = ViewLogStreamMetricSource.Location,
            });
        }
    }
}
