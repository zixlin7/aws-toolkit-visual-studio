using System;
using System.Runtime.Versioning;

using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.Solutions
{
    public class Project : IEquatable<Project>
    {
        public string Name { get; }
        public string Path { get; }
        public Guid Guid { get; }
        public FrameworkName TargetFramework { get; }

        public Project(string name, string path, Guid guid, FrameworkName targetFramework)
        {
            Name = name;
            Path = path;
            Guid = guid;
            TargetFramework = targetFramework;
        }

        public bool IsNetCore()
        {
            return FrameworkNameHelper.IsDotNetCore(TargetFramework);
        }

        public bool IsNetFramework()
        {
            return FrameworkNameHelper.IsDotNetFramework(TargetFramework);
        }

        public bool Equals(Project other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name && Path == other.Path && Guid == other.Guid && TargetFramework == other.TargetFramework;
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
                hashCode = (hashCode * 397) ^ Guid.GetHashCode();
                hashCode = (hashCode * 397) ^ (TargetFramework != null ? TargetFramework.GetHashCode() : 0);
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

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Path)}: {Path}, {nameof(Guid)}: {Guid}, {nameof(TargetFramework)}: {TargetFramework}";
        }
    }
}
