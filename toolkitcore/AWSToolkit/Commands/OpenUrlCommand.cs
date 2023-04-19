using System;
using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.Commands
{
    /// <summary>
    /// Command to open links/urls in the browser and record appropriate metric associated
    /// </summary>
    public class OpenUrlCommandFactory
    {
        private OpenUrlCommandFactory() { }

        /// <summary>
        /// Overload to specify a url to be opened in the browser
        /// </summary>
        public static ICommand Create(ToolkitContext toolkitContext, string url,
            Action<ITelemetryLogger> recordMetric = null)
        {
            return Create(toolkitContext, null, url, recordMetric);
        }

        /// <summary>
        /// Overload to specify source of the action as part of metric metadata
        /// </summary>
        public static ICommand Create(ToolkitContext toolkitContext, BaseMetricSource metricSource,
            Action<ITelemetryLogger> recordMetric = null)
        {
            return Create(toolkitContext, metricSource, null, recordMetric);
        }

        private static ICommand Create(ToolkitContext toolkitContext, BaseMetricSource metricSource, string url,
            Action<ITelemetryLogger> recordMetric = null)
        {
            void OpenUrlLink(object obj)
            {
                var param = url ?? obj;
                OpenUrl(param, toolkitContext, metricSource, recordMetric);
            }

            return CreateCommand(toolkitContext, OpenUrlLink);
        }

        private static ICommand CreateCommand(ToolkitContext toolkitContext, Action<object> openUrlAction)
        {
            var command = new RelayCommand(openUrlAction);
            return new ShowExceptionAndForgetCommand(command, toolkitContext.ToolkitHost);
        }

        private static void OpenUrl(object urlObj, ToolkitContext toolkitContext, BaseMetricSource metricSource,
            Action<ITelemetryLogger> recordMetric = null)
        {
            try
            {
                var url = urlObj as string;
                void Invoke() => toolkitContext.ToolkitHost.OpenInBrowser(url, preferInternalBrowser: false);

                void Record(ITelemetryLogger _)
                {
                    if (recordMetric == null)
                    {
                        toolkitContext.TelemetryLogger.RecordAwsOpenUrl(new AwsOpenUrl()
                        {
                            AwsAccount = toolkitContext.ConnectionManager.ActiveAccountId,
                            AwsRegion = MetadataValue.NotApplicable,
                            Url = url,
                            Result = Result.Succeeded,
                            Source = metricSource?.Location
                        });
                        return;
                    }

                    recordMetric(toolkitContext.TelemetryLogger);
                }

                toolkitContext.TelemetryLogger.InvokeAndRecord(Invoke, Record);
            }
            catch (Exception e)
            {
                throw new CommandException("Failed to open url in browser", e);
            }
        }
    }
}
