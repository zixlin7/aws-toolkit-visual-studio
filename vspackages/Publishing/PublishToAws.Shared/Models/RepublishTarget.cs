using Amazon.AWSToolkit.Publish.Util;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// Represents an existing destination for publishing a user's application to AWS.
    /// Used by the "Existing Target" UI in "Select Targets".
    /// </summary>
    public class RepublishTarget : PublishDestinationBase
    {
        public string ExistingDeploymentId { get; }

        public bool DeployedToRecently { get; set; }

        public string Category => GetCategory();

        public RepublishTarget(ExistingDeploymentSummary deploymentStack)
        {
            Name = deploymentStack?.Name;
            Service = deploymentStack?.TargetService;
            DeploymentArtifact = deploymentStack?.DeploymentType.AsDeploymentArtifact() ?? DeploymentArtifact.Unknown;
            RecipeId = deploymentStack?.RecipeId;
            RecipeName = deploymentStack?.RecipeName;
            Description = deploymentStack?.Description;
            ShortDescription = deploymentStack?.ShortDescription;
            ExistingDeploymentId = deploymentStack?.ExistingDeploymentId;
        }

        public string GetCategory()
        {
            return DeployedToRecently ? "Recently deployed" : "Other Targets";
        }

        public bool Equals(RepublishTarget other)
        {
            return base.Equals(other) &&
                   ExistingDeploymentId == other.ExistingDeploymentId;
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
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (ExistingDeploymentId != null ? ExistingDeploymentId.GetHashCode() : 0);
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
