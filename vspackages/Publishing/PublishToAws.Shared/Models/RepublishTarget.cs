using System;
using System.Windows.Media;

using Amazon.AWSToolkit.Publish.Util;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// Represents an existing destination for publishing a user's application to AWS.
    /// Used by the "Existing Target" UI in "Select Targets".
    /// </summary>
    public class RepublishTarget : IEquatable<RepublishTarget>
    {
        public string Name { get; }

        public string Service { get; }

        public string RecipeId { get; }

        public string RecipeName { get; }

        public string Description { get; }

        public string ShortDescription { get; }

        public ImageSource ServiceIcon => RecipeServiceImageResolver.GetServiceImage(Service);

        /// <summary>
        /// Whether or not this is the most recommended republish target.
        /// </summary>
        public bool IsRecommended { get; set; } = false;

        public bool DeployedToRecently { get; set; }

        public string Category => GetCategory();

        public RepublishTarget(ExistingDeploymentSummary deploymentStack)
        {
            Name = deploymentStack?.Name;
            Service = deploymentStack?.TargetService;
            RecipeId = deploymentStack?.RecipeId;
            RecipeName = deploymentStack?.RecipeName;
            Description = deploymentStack?.Description;
            ShortDescription = deploymentStack?.ShortDescription;
        }

        public string GetCategory()
        {
            return DeployedToRecently ? "Recently deployed" : "Other Targets";
        }

        public bool Equals(RepublishTarget other)
        {
            return Name == other.Name && Service == other.Service && RecipeId == other.RecipeId && RecipeName == other.RecipeName && Description == other.Description;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((RepublishTarget) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Service != null ? Service.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (RecipeId != null ? RecipeId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (RecipeName != null ? RecipeName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(RepublishTarget left, RepublishTarget right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RepublishTarget left, RepublishTarget right)
        {
            return !Equals(left, right);
        }
    }
}
