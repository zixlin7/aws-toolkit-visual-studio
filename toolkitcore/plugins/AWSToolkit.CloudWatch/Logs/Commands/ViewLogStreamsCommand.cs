using System;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Logs.Models;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Telemetry.Model;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Logs.Commands
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
            CloudWatchTelemetry.RecordOpenLogGroup(result, ViewLogGroupMetricSource, connectionSettings, toolkitContext);
        }

        private static bool ViewLogStreams(object parameter, AwsConnectionSettings connectionSettings,
            ToolkitContext toolkitContext)
        {
            try 
            {
                if (!(parameter is string logGroup))
                {
                    throw new ArgumentException($"Parameter is not of expected type: {typeof(string)}");
                }
                var logStreamsViewer =
                    toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(ILogStreamsViewer)) as
                        ILogStreamsViewer;
                if (logStreamsViewer == null)
                {
                    throw new Exception("Unable to load CloudWatch log group data source");
                }

                logStreamsViewer.View(logGroup, connectionSettings);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error viewing log streams", ex);
                toolkitContext.ToolkitHost.ShowError($"Error viewing log streams: {ex.Message}");

                return false;
            }
        }
    }
}
