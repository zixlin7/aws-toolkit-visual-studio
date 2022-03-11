using System;

using Amazon.CloudWatchLogs;

namespace Amazon.AWSToolkit.CloudWatch
{
    /// <summary>
    /// Represents common properties/state required by CloudWatchLogsRepository to make required AWS SDK API calls
    /// The intent of this class is to pass core components around without a lengthy parameter list
    /// </summary>
    public class CloudWatchLogsProperties
    {
        public IAmazonCloudWatchLogs CloudWatchLogsClient { get; set; }

        public string NextToken { get; set; }

        public string LogGroup { get; set; } = string.Empty;

        public string LogStream { get; set; } = string.Empty;

        public string FilterText { get; set; } = string.Empty;

        public DateTime StartTime { get; set; } 

        public DateTime EndTime { get; set; }
    }
}
