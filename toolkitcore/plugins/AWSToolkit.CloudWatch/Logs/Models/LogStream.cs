using System;

namespace Amazon.AWSToolkit.CloudWatch.Logs.Models
{
    /// <summary>
    /// Represents a log stream with Last event timestamp represented in Local System Time
    /// </summary>
    public class LogStream
    {
        public string Name { get; set; }

        public string Arn { get; set; }

        public DateTime LastEventTime { get; set; }

        protected bool Equals(LogStream other)
        {
            return Name == other.Name && Arn == other.Arn && LastEventTime.Equals(other.LastEventTime);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LogStream)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Arn != null ? Arn.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ LastEventTime.GetHashCode();
                return hashCode;
            }
        }
    }
}
