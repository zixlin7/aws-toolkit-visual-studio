using System;

namespace Amazon.AWSToolkit.Solutions
{
    public class Project : IEquatable<Project>
    {
        public string Name { get; }
        public string Path { get; }
        public ProjectType Type { get; }

        public Project(string name, string path, ProjectType type)
        {
            Name = name;
            Path = path;
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
            return Name == other.Name && Path == other.Path && Type == other.Type;
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
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Path != null ? Path.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Type;
                return hashCode;
            }
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
