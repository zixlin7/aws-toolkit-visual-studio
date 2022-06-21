using System;

namespace Amazon.AWSToolkit.CloudWatch.Models
{
    /// <summary>
    /// Toolkit representation of a filter log events request
    /// </summary>
    public class FilterLogEventsRequest
    {
        public string LogGroup { get; set; } = string.Empty;

        public string LogStream { get; set; } = string.Empty;

        public string NextToken { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string FilterText { get; set; } = string.Empty;
    }
}
