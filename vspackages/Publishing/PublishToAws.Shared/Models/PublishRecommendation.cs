using Amazon.AWSToolkit.Publish.Util;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// Represents a destination and/or technique for publishing a user's application to AWS.
    /// Used by the "Select Target" UI.
    /// </summary>
    public class PublishRecommendation : PublishDestinationBase
    {
        public PublishRecommendation(RecommendationSummary recommendation)
        {
            RecipeId = recommendation?.RecipeId;
            RecipeName = recommendation?.Name;
            DeploymentArtifact = recommendation?.DeploymentType.AsDeploymentArtifact() ?? DeploymentArtifact.Unknown;
            Name = recommendation?.Name;
            Description = recommendation?.Description;
            Service = recommendation?.TargetService;
            ShortDescription = recommendation?.ShortDescription;
        }
    }
}
