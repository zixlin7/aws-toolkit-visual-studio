﻿using System.Linq;

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
