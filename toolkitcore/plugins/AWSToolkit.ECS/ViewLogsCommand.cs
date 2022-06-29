using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.View;
using Amazon.AWSToolkit.Telemetry.Model;

using log4net;

using InvalidOperationException = System.InvalidOperationException;

namespace Amazon.AWSToolkit.ECS
{
    /// <summary>
    /// Command to view logs related to an ECS task
    /// </summary>
    public class ViewLogsCommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ViewLogsCommand));

        private static readonly BaseMetricSource ViewLogStreamMetricSource =
            MetricSources.CloudWatchLogsMetricSource.ClusterTaskView;

        public static ICommand Create(ToolkitContext toolkitContext, AwsConnectionSettings connectionSettings)
        {
            return new RelayCommand(parameter => Execute(parameter, connectionSettings, toolkitContext));
        }

        private static void Execute(object parameter, AwsConnectionSettings connectionSettings,
            ToolkitContext toolkitContext)
        {
            var result = ViewLogEvents(parameter, connectionSettings, toolkitContext);
            CloudWatchTelemetry.RecordOpenLogStream(result, ViewLogStreamMetricSource, connectionSettings,
                toolkitContext);
        }

        private static bool ViewLogEvents(object parameter, AwsConnectionSettings connectionSettings,
            ToolkitContext toolkitContext)
        {
            try
            {
                if (!(parameter is Dictionary<string, LogProperties> containerToLogs))
                {
                    throw new ArgumentException(
                        $"Parameter is not of expected type: {typeof(Dictionary<string, LogProperties>)}");
                }

                if (!containerToLogs.Any())
                {
                    throw new InvalidOperationException("No containers to view logs from found in the ECS Task.");
                }

                var selectedContainer = GetContainerToViewLogs(containerToLogs, toolkitContext);
                if (selectedContainer == null || !containerToLogs.TryGetValue(selectedContainer, out var properties))
                {
                    return false;
                }

                ViewLogEvents(properties, connectionSettings, toolkitContext);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error viewing log events", ex);
                toolkitContext.ToolkitHost.ShowError($"Error viewing log events: {ex.Message}");

                return false;
            }
        }

        private static string GetContainerToViewLogs(Dictionary<string, LogProperties> containerToLogs,
            ToolkitContext toolkitContext)
        {
            if (containerToLogs.Count == 1)
            {
                return containerToLogs.First().Key;
            }

            //show selection dialog if more than one container is present in the task
            var control = new ContainerSelectionControl()
            {
                Containers = new ObservableCollection<string>(containerToLogs.Keys),
                Container = containerToLogs.Keys.First()
            };

            var result = toolkitContext.ToolkitHost.ShowInModalDialogWindow(control, MessageBoxButton.OKCancel);
            return result ? control.Container : null;
        }

        private static void ViewLogEvents(LogProperties properties, AwsConnectionSettings connectionSettings,
            ToolkitContext toolkitContext)
        {
            var logEventsViewer =
                toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(ILogEventsViewer)) as
                    ILogEventsViewer;
            if (logEventsViewer == null)
            {
                throw new Exception("Unable to load CloudWatch log stream data source");
            }

            logEventsViewer.View(properties.LogGroup, properties.LogStream, connectionSettings);
        }
    }
}
