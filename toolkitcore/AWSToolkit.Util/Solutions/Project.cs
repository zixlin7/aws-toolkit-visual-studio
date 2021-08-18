using System;

namespace Amazon.AWSToolkit.Solutions
{
    public class Project : IEquatable<Project>
    {
        public ProjectType Type { get; }

        public Project(ProjectType type)
        {
            Type = type;
        }

        public bool IsNetCore()
        {
            return ProjectType.NetCore.Equals(Type);
        }

        public bool IsNetFramework()
        {
            return ProjectType.NetFramework.Equals(Type);
        }

        public bool IsUnknown()
        {
            return ProjectType.Unknown.Equals(Type);
        }

        public bool Equals(Project other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Project)obj);
        }

        public override int GetHashCode()
        {
            return (int)Type;
        }

        public static bool operator ==(Project left, Project right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Project left, Project right)
        {
            return !Equals(left, right);
        }
    }
}
