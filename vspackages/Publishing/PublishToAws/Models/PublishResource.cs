using System.Collections.Generic;

namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// Represents a resource that is created once an application is published.
    /// Used by the "Publish Application View" UI.
    /// </summary>
    public class PublishResource
    {
        public string Id { get; }

        public string Type { get; }

        public string Description { get; }

        public IDictionary<string, string> Data { get; }

        public PublishResource(string id, string type, string description, IDictionary<string, string> data)
        {
            Id = id;
            Type = type;
            Description = description;
            Data = data;
        }

        protected bool Equals(PublishResource other)
        {
            return Id == other.Id && Type == other.Type && Description == other.Description && Equals(Data, other.Data);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PublishResource) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Data != null ? Data.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
