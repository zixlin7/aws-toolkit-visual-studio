using System;

namespace AWSDeployment
{
    public class HostManagerEvent
    {
        public HostManagerEvent(DateTime timestamp, string source, string message, string severity)
        {
            this.Timestamp = timestamp;
            this.Source = source;
            this.Message = message;
            this.Severity = severity;
        }

        public DateTime Timestamp { get; }
        public string Source { get; }
        public string Message { get; }
        public string Severity { get; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", this.Timestamp, this.Message);
        }
    }
}
