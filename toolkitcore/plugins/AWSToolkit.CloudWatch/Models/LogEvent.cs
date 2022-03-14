using System;

namespace Amazon.AWSToolkit.CloudWatch.Models
{
    /// <summary>
    /// Represents a log event with timestamp represented in Local System Time
    /// </summary>
    public class LogEvent
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }

        protected bool Equals(LogEvent other)
        {
            return Message == other.Message && Timestamp.Equals(other.Timestamp);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LogEvent)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Message != null ? Message.GetHashCode() : 0) * 397) ^ Timestamp.GetHashCode();
            }
        }
    }
}
