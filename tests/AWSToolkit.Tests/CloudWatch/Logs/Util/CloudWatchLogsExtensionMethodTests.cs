using System;

using Amazon.AWSToolkit.CloudWatch.Logs.Models;
using Amazon.AWSToolkit.CloudWatch.Logs.Util;
using Amazon.CloudWatchLogs.Model;

using Xunit;

using CloudWatchLogGroup = Amazon.CloudWatchLogs.Model.LogGroup;
using CloudWatchLogStream = Amazon.CloudWatchLogs.Model.LogStream;
using LogGroup = Amazon.AWSToolkit.CloudWatch.Logs.Models.LogGroup;
using LogStream = Amazon.AWSToolkit.CloudWatch.Logs.Models.LogStream;

namespace AWSToolkit.Tests.CloudWatch.Logs.Util
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

        [Theory]
        [InlineData("sample-event-message")]
        [InlineData("    sample-event-message")]
        [InlineData("sample-event-message     ")]
        [InlineData(" sample-event-message ")]
        public void ToLogEvent(string message)
        {
            var cloudWatchLogEvent = new OutputLogEvent() { Message = message, Timestamp = new DateTime(2022, 01, 01) };
            var expected = new LogEvent
            {
                Message = "sample-event-message",
                Timestamp = new DateTime(2022, 01, 01, 00, 00, 00, DateTimeKind.Utc).ToLocalTime()
            };
            Assert.Equal(expected, cloudWatchLogEvent.ToLogEvent());
        }

        [Theory]
        [InlineData("sample-event-message")]
        [InlineData("    sample-event-message")]
        [InlineData("sample-event-message     ")]
        [InlineData(" sample-event-message ")]
        public void ToLogEvent_Filtered(string message)
        {
            var cloudWatchLogEvent = new FilteredLogEvent() { Message = message, Timestamp = 1640995200000 };
            var expected = new LogEvent
            {
                Message = "sample-event-message",
                Timestamp = new DateTime(2022, 01, 01, 00, 00, 00, DateTimeKind.Utc).ToLocalTime()
            };
            Assert.Equal(expected, cloudWatchLogEvent.ToLogEvent());
        }
    }
}
