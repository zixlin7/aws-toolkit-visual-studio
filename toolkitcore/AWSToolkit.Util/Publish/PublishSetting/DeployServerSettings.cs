using System;

namespace Amazon.AWSToolkit.Publish.PublishSetting
{
    public class DeployServerSettings : IEquatable<DeployServerSettings>
    {
        public PortRange PortRange { get; }

        public DeployServerSettings(PortRange portRange)
        {
            PortRange = portRange;
        }

        public static DeployServerSettings CreateDefault()
        {
            return new DeployServerSettings(new PortRange(10000, 10100));
        }

        public bool Equals(DeployServerSettings other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(PortRange, other.PortRange);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DeployServerSettings) obj);
        }

        public override int GetHashCode()
        {
            return (PortRange != null ? PortRange.GetHashCode() : 0);
        }

        public static bool operator ==(DeployServerSettings left, DeployServerSettings right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DeployServerSettings left, DeployServerSettings right)
        {
            return !Equals(left, right);
        }
    }
}
