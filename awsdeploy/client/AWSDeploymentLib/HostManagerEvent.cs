using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public DateTime Timestamp { get; private set; }
        public string Source { get; private set; }
        public string Message { get; private set; }
        public string Severity { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", this.Timestamp, this.Message);
        }
    }
}
