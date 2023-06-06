using System;
using System.Diagnostics;

namespace Amazon.AWSToolkit.Regions
{
    [DebuggerDisplay("{PartitionId}: {Id} / {DisplayName}")]
    public class ToolkitRegion : IEquatable<ToolkitRegion>
    {
        public const string DefaultRegionId = "us-east-1";

        /// <summary>
        /// Region Id (eg: "us-west-2")
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// User friendly name (eg: "US West (Oregon)")
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Id of parent partition (eg: "aws")
        /// </summary>
        public string PartitionId { get; set; }

        public bool Equals(ToolkitRegion other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Id, other.Id, StringComparison.InvariantCulture) &&
                   string.Equals(DisplayName, other.DisplayName, StringComparison.InvariantCulture) &&
                   string.Equals(PartitionId, other.PartitionId, StringComparison.InvariantCulture);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            if (obj.GetType() != this.GetType()) return false;

            return Equals((ToolkitRegion) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Id != null ? StringComparer.InvariantCulture.GetHashCode(Id) : 0);
                hashCode = (hashCode * 397) ^ (DisplayName != null ? StringComparer.InvariantCulture.GetHashCode(DisplayName) : 0);
                hashCode = (hashCode * 397) ^ (PartitionId != null ? StringComparer.InvariantCulture.GetHashCode(PartitionId) : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ToolkitRegion left, ToolkitRegion right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ToolkitRegion left, ToolkitRegion right)
        {
            return !Equals(left, right);
        }
    }
}
