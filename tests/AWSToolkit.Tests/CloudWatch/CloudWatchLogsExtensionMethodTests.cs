using System;

using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.Util;
using Amazon.CloudWatchLogs.Model;

using Xunit;

using CloudWatchLogGroup = Amazon.CloudWatchLogs.Model.LogGroup;
using CloudWatchLogStream = Amazon.CloudWatchLogs.Model.LogStream;
using LogGroup = Amazon.AWSToolkit.CloudWatch.Models.LogGroup;
using LogStream = Amazon.AWSToolkit.CloudWatch.Models.LogStream;

namespace AWSToolkit.Tests.CloudWatch
{
    public class CloudWatchLogsExtensionMethodTests
    {
        [Fact]
        public void ToLogGroup()
        {
            var cloudWatchLogGroup = new CloudWatchLogGroup() { LogGroupName = "sample-group", Arn = "sample-group-arn" };
            var expected = new LogGroup { Name = "sample-group", Arn = "sample-group-arn" };
            Assert.Equal(expected, cloudWatchLogGroup.ToLogGroup());
        }

        [Fact]
        public void ToLogStream()
        {
            var cloudWatchLogStream = new CloudWatchLogStream()
            {
                LogStreamName = "sample-stream",
                Arn = "sample-stream-arn",
                LastEventTimestamp = new DateTime(2022, 01, 01, 00, 00, 00, DateTimeKind.Utc)
            };
            var expected = new LogStream
            {
                Name = "sample-stream", Arn = "sample-stream-arn", LastEventTime = new DateTime(2022, 01, 01).ToLocalTime()
            };
            Assert.Equal(expected, cloudWatchLogStream.ToLogStream());
        }

        [Fact]
        public void ToLogEvent()
        {
            var cloudWatchLogEvent = new FilteredLogEvent() { Message = "sample-event-message", Timestamp = 1640995200000 };
            var expected = new LogEvent
            {
                Message = "sample-event-message",
                Timestamp = new DateTime(2022, 01, 01, 00, 00, 00, DateTimeKind.Utc).ToLocalTime()
            };
            Assert.Equal(expected, cloudWatchLogEvent.ToLogEvent());
        }
    }
}
