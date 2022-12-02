using Amazon.CodeCatalyst.Model;

namespace Amazon.AWSToolkit.CodeCatalyst.Models
{
    internal class CodeCatalystProject : ICodeCatalystProject
    {
        public string Name { get; }

        public string SpaceName { get; }

        public string DisplayName { get; }

        public string Description { get; }

        internal CodeCatalystProject(string name, string spaceName, string displayName, string description)
        {
            Arg.NotNull(name, nameof(name));
            Arg.NotNull(spaceName, nameof(spaceName));

            Name = name;
            SpaceName = spaceName;
            DisplayName = displayName;
            Description = description;
        }

        internal CodeCatalystProject(string spaceName, ProjectSummary summary)
            : this(summary.Name, spaceName, summary.DisplayName, summary.Description) { }

        protected bool Equals(CodeCatalystProject other)
        {
            return Name == other.Name && SpaceName == other.SpaceName && DisplayName == other.DisplayName && Description == other.Description;
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

            return Equals((CodeCatalystProject) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SpaceName != null ? SpaceName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DisplayName != null ? DisplayName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
