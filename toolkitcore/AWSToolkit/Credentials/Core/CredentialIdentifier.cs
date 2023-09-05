using System;
using System.Diagnostics;

namespace Amazon.AWSToolkit.Credentials.Core
{
    [DebuggerDisplay("{Id}")]
    public abstract class CredentialIdentifier : ICredentialIdentifier, IEquatable<CredentialIdentifier>
    {
        public string Id { get; protected set; }

        public string ProfileName { get; protected set; }

        public string DisplayName { get; protected set; }

        public string ShortName { get; protected set; }

        public string FactoryId { get; protected set; }

        public bool Equals(CredentialIdentifier other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id == other.Id && ProfileName == other.ProfileName && DisplayName == other.DisplayName && ShortName == other.ShortName && FactoryId == other.FactoryId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((CredentialIdentifier)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (ProfileName?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (DisplayName?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (ShortName?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (FactoryId?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }
}
