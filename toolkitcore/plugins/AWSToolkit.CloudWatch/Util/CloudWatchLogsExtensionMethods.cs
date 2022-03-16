using System;

using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.Util;
using Amazon.CloudWatchLogs.Model;

using CloudWatchLogGroup = Amazon.CloudWatchLogs.Model.LogGroup;
using CloudWatchLogStream = Amazon.CloudWatchLogs.Model.LogStream;
using LogGroup = Amazon.AWSToolkit.CloudWatch.Models.LogGroup;
using LogStream = Amazon.AWSToolkit.CloudWatch.Models.LogStream;

namespace Amazon.AWSToolkit.CloudWatch.Util
{
    public static class CloudWatchLogsExtensionMethods
    {
        public static LogGroup ToLogGroup(this CloudWatchLogGroup cloudWatchLogGroup)
        {
            return new LogGroup
            {
                Name = cloudWatchLogGroup.LogGroupName,
                Arn = cloudWatchLogGroup.Arn
            };
        }

        public static LogStream ToLogStream(this CloudWatchLogStream cloudWatchLogStream)
        {
            return new LogStream
            {
                Name = cloudWatchLogStream.LogStreamName,
                Arn = cloudWatchLogStream.Arn,
                LastEventTime = cloudWatchLogStream.LastEventTimestamp.ToLocalTime()
            };
        }

        public static LogEvent ToLogEvent(this FilteredLogEvent cloudWatchLogEvent)
        {
            return new LogEvent
            {
                Message = cloudWatchLogEvent.Message,
                Timestamp = DateTimeUtil.ConvertUnixToDateTime(cloudWatchLogEvent.Timestamp, TimeZoneInfo.Local)
            };
        }
    }
}
