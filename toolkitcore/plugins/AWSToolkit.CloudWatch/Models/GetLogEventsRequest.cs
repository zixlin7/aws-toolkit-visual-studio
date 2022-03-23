using System;

namespace Amazon.AWSToolkit.CloudWatch.Models
{
    /// <summary>
    /// Toolkit representation of a get log events request
    /// </summary>
    public class GetLogEventsRequest
    {
        public string LogGroup { get; set; } = string.Empty;

        public string LogStream { get; set; } = string.Empty;

        public string NextToken { get; set; }

        public string FilterText { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }
    }
}
