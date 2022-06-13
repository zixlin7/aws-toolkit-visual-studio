using System.Linq;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch
{
    public static class TelemetryFixtureExtensionMethods
    {
        public static void VerifyRecordCloudWatchLogsOpen(this TelemetryFixture telemetryFixture,
            Result expectedResult, CloudWatchResourceType expectedResourceType)
        {
            var metric = telemetryFixture.LoggedMetrics
                .SelectMany(m => m.Data)
                .Single(datum => datum.MetricName == "cloudwatchlogs_open");

            Assert.Equal(expectedResult.ToString(), metric.Metadata["result"]);
            Assert.Equal(expectedResourceType.ToString(), metric.Metadata["cloudWatchResourceType"]);
        }
    }
}
