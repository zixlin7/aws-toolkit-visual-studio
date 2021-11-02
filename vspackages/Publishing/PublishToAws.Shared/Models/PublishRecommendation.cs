using System;
using System.Windows.Media;

using Amazon.AWSToolkit.Publish.Util;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// Represents a destination and/or technique for publishing a user's application to AWS.
    /// Used by the "Select Target" UI.
    /// </summary>
    public class PublishRecommendation : IEquatable<PublishRecommendation>
    {
        public string RecipeId { get; }

        public string Name { get; }

        public string Description { get; }

        public string ShortDescription { get; }

        public string Service { get; }

        public ImageSource ServiceIcon => RecipeServiceImageResolver.GetServiceImage(Service);

        /// <summary>
        /// Whether or not this is the most recommended publish target.
        /// </summary>
        public bool IsRecommended { get; set; } = false;

        public PublishRecommendation(RecommendationSummary recommendation)
        {
            RecipeId = recommendation?.RecipeId;
            Name = recommendation?.Name;
            Description = recommendation?.Description;
            Service = recommendation?.TargetService;
            ShortDescription = recommendation?.ShortDescription;
        }

        public bool Equals(PublishRecommendation other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return RecipeId == other.RecipeId && Name == other.Name && Description == other.Description && Service == other.Service && IsRecommended == other.IsRecommended;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PublishRecommendation) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (RecipeId != null ? RecipeId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Service != null ? Service.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsRecommended.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(PublishRecommendation left, PublishRecommendation right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PublishRecommendation left, PublishRecommendation right)
        {
            return !Equals(left, right);
        }
    }
}
