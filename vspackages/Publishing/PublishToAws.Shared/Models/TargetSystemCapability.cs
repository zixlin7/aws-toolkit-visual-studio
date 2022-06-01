using System;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Models
{
    public class TargetSystemCapability : IEquatable<TargetSystemCapability>
    {
        public string Name { get; }

        public string Message { get;  }

        public string InstallationUrl { get; }

        public bool HasUrl => !string.IsNullOrEmpty(InstallationUrl) && InstallationUrl.StartsWith("http");

        public TargetSystemCapability(SystemCapabilitySummary systemCapabilitySummary)
        {
            Name = systemCapabilitySummary.Name;
            Message = systemCapabilitySummary.Message;
            InstallationUrl = systemCapabilitySummary.InstallationUrl;
        }

        public bool Equals(TargetSystemCapability other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Name == other.Name && Message == other.Message && InstallationUrl == other.InstallationUrl;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((TargetSystemCapability) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Message != null ? Message.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (InstallationUrl != null ? InstallationUrl.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(TargetSystemCapability left, TargetSystemCapability right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TargetSystemCapability left, TargetSystemCapability right)
        {
            return !Equals(left, right);
        }
    }
}
