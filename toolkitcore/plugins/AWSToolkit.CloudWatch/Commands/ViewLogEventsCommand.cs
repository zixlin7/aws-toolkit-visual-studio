using System;
using System.CodeDom;
using System.Linq;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
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
            CloudWatchTelemetry.RecordOpenLogStream(result, ViewLogStreamMetricSource, connectionSettings, toolkitContext);
        }

        private static bool ViewLogEvents(object parameter,
            AwsConnectionSettings connectionSettings, ToolkitContext toolkitContext)
        {
            try
            {
                var parameters = parameter as object[];
                if (parameters == null || parameters.Count() != 2)
                {
                    throw new ArgumentException($"Expected parameters: 2, Found: {parameters?.Count()}");
                }

                if (parameters.Any(x => !(x is string)))
                {
                    throw new ArgumentException($"Parameters are not of expected type: {typeof(string)}");
                }

                var logGroup = parameters[0] as string;
                var logStream = parameters[1] as string;

                var logEventsViewer =
                    toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(ILogEventsViewer)) as
                        ILogEventsViewer;
                if (logEventsViewer == null)
                {
                    throw new Exception("Unable to load CloudWatch log stream data source");
                }

                logEventsViewer.View(logGroup, logStream, connectionSettings);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error viewing log events", ex);
                toolkitContext.ToolkitHost.ShowError($"Error viewing log events: {ex.Message}");

                return false;
            }
        }
    }
}
