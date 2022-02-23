using System;
using System.Windows.Media;

using Amazon.AWSToolkit.Publish.Util;

namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// Represents a destination and technique for deploying an application.
    /// </summary>
    public abstract class PublishDestinationBase : IEquatable<PublishDestinationBase>
    {
        public string Name { get; protected set; }

        /// <summary>
        /// Whether or not this is the most recommended publish target.
        /// </summary>
        public bool IsRecommended { get; set; } = false;

        public string RecipeId { get; protected set; }
        public string RecipeName { get; protected set; }

        public string Description { get; protected set; }
        public string ShortDescription { get; protected set; }

        public DeploymentArtifact DeploymentArtifact { get; protected set; }

        public string Service { get; protected set; }
        public ImageSource ServiceIcon => RecipeServiceImageResolver.GetServiceImage(Service);


        public bool Equals(PublishDestinationBase other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name &&
                   IsRecommended == other.IsRecommended &&
                   RecipeId == other.RecipeId &&
                   RecipeName == other.RecipeName &&
                   Description == other.Description &&
                   ShortDescription == other.ShortDescription &&
                   DeploymentArtifact == other.DeploymentArtifact &&
                   Service == other.Service;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PublishDestinationBase) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsRecommended.GetHashCode();
                hashCode = (hashCode * 397) ^ (RecipeId != null ? RecipeId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (RecipeName != null ? RecipeName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ShortDescription != null ? ShortDescription.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DeploymentArtifact != null ? DeploymentArtifact.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Service != null ? Service.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(PublishDestinationBase left, PublishDestinationBase right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PublishDestinationBase left, PublishDestinationBase right)
        {
            return !Equals(left, right);
        }
    }
}
