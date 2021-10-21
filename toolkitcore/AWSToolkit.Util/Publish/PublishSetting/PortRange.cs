using System;

namespace Amazon.AWSToolkit.Publish.PublishSetting
{
    public class PortRange : IEquatable<PortRange>
    {
        public int Start { get; }
        public int End { get; }

        public PortRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public bool Equals(PortRange other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Start == other.Start && End == other.End;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PortRange) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start * 397) ^ End;
            }
        }

        public static bool operator ==(PortRange left, PortRange right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PortRange left, PortRange right)
        {
            return !Equals(left, right);
        }
    }
}
