using Amazon.CodeCatalyst.Model;

namespace Amazon.AWSToolkit.CodeCatalyst.Models
{
    internal class CodeCatalystSpace : ICodeCatalystSpace
    {
        public string Name { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public string RegionId { get; }

        internal CodeCatalystSpace(string name, string displayName, string description, string regionId)
        {
            Arg.NotNull(name, nameof(name));
            Arg.NotNull(regionId, nameof(regionId));

            Name = name;
            DisplayName = displayName;
            Description = description;
            RegionId = regionId;
        }

        internal CodeCatalystSpace(SpaceSummary summary)
            : this(summary.Name, summary.DisplayName, summary.Description, summary.RegionName) { }

        protected bool Equals(CodeCatalystSpace other)
        {
            return Name == other.Name && DisplayName == other.DisplayName && Description == other.Description && RegionId == other.RegionId;
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

            return Equals((CodeCatalystSpace) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DisplayName != null ? DisplayName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (RegionId != null ? RegionId.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
