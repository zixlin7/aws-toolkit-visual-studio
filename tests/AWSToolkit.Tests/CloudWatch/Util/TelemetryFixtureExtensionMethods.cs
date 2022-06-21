using System.Linq;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch
{
    public static class TelemetryFixtureExtensionMethods
    {
        public static void VerifyRecordCloudWatchLogsDelete(this TelemetryFixture telemetryFixture,
            Result expectedResult, CloudWatchResourceType expectedResourceType)
        {
            var metric = telemetryFixture.LoggedMetrics
                .SelectMany(m => m.Data)
                .Single(datum => datum.MetricName == "cloudwatchlogs_delete");

            Assert.Equal(expectedResult.ToString(), metric.Metadata["result"]);
            Assert.Equal(expectedResourceType.ToString(), metric.Metadata["cloudWatchResourceType"]);
        }

        public static void VerifyRecordCloudWatchLogsDownload(this TelemetryFixture telemetryFixture,
            Result expectedResult)
        {
            var metric = telemetryFixture.LoggedMetrics
                .SelectMany(m => m.Data)
                .Single(datum => datum.MetricName == "cloudwatchlogs_download");

            Assert.Equal(expectedResult.ToString(), metric.Metadata["result"]);
            Assert.Equal(CloudWatchResourceType.LogStream.ToString(), metric.Metadata["cloudWatchResourceType"]);
        }

        public static void VerifyRecordCloudWatchLogsFilter(this TelemetryFixture telemetryFixture,
            CloudWatchResourceType expectedResourceType,
            int expectedFilterByTextCount,
            int expectedFilterByTimeCount)
        {
            var metrics = telemetryFixture.LoggedMetrics
                .SelectMany(m => m.Data)
                .Where(datum => datum.MetricName == "cloudwatchlogs_filter")
                .Where(datum => datum.Metadata.TryGetValue("cloudWatchResourceType", out string resourceType) && resourceType == expectedResourceType.ToString())
                .ToList();

            Assert.Equal(expectedFilterByTextCount, metrics.Count(m => m.Metadata.TryGetValue("hasTextFilter", out string hasTextFilter) && hasTextFilter == "true"));
            Assert.Equal(expectedFilterByTimeCount, metrics.Count(m => m.Metadata.TryGetValue("hasTimeFilter", out string hasTimeFilter) && hasTimeFilter == "true"));
        }
        
        public static void VerifyRecordCloudWatchLogsOpen(this TelemetryFixture telemetryFixture,
            Result expectedResult, CloudWatchResourceType expectedResourceType)
        {
            var metric = telemetryFixture.LoggedMetrics
                .SelectMany(m => m.Data)
                .Single(datum => datum.MetricName == "cloudwatchlogs_open");

            Assert.Equal(expectedResult.ToString(), metric.Metadata["result"]);
            Assert.Equal(expectedResourceType.ToString(), metric.Metadata["cloudWatchResourceType"]);
        }

        public static void VerifyRecordCloudWatchLogsRefresh(this TelemetryFixture telemetryFixture,
            CloudWatchResourceType expectedResourceType)
        {
            var metric = telemetryFixture.LoggedMetrics
                .SelectMany(m => m.Data)
                .Single(datum => datum.MetricName == "cloudwatchlogs_refresh");

            Assert.Equal(expectedResourceType.ToString(), metric.Metadata["cloudWatchResourceType"]);
        }
    }
}
