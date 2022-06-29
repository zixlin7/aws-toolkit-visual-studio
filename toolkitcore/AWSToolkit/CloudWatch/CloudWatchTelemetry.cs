using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.CloudWatch
{
    /// <summary>
    /// Records CloudWatch Logs related telemetry events
    /// </summary>
    public static class CloudWatchTelemetry
    {
        public static void RecordOpenLogGroup(bool openResult, BaseMetricSource metricSource,
            AwsConnectionSettings connectionSettings, ToolkitContext toolkitContext)
        {
            toolkitContext.TelemetryLogger.RecordCloudwatchlogsOpen(new CloudwatchlogsOpen()
            {
                AwsAccount =
                    MetricsMetadata.AccountIdOrDefault(
                        connectionSettings.GetAccountId(toolkitContext.ServiceClientManager)),
                AwsRegion = MetricsMetadata.RegionOrDefault(connectionSettings.Region),
                CloudWatchLogsPresentation = CloudWatchLogsPresentation.Ui,
                CloudWatchResourceType = CloudWatchResourceType.LogGroup,
                Result = openResult ? Result.Succeeded : Result.Failed,
                ServiceType = metricSource.Service,
                Source = metricSource.Location,
            });
        }

        public static void RecordOpenLogStream(bool openResult, BaseMetricSource metricSource,
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
                ServiceType = metricSource.Service,
                Source = metricSource.Location
            });
        }
    }
}
