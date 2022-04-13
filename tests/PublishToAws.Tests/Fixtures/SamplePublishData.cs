using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Publish.Models;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Tests.Publishing.Fixtures
{
    public class SamplePublishData
    {
        private static readonly IDictionary<string, RecipeSummary> SampleRecipes =
            new Dictionary<string, RecipeSummary>()
            {
                {
                    "recipe-1",
                    new RecipeSummary()
                    {
                        Name = "Sample Recipe 1",
                        TargetService = "Sample Service 1",
                        Id = "recipe-1",
                        ShortDescription = "short-description-1",
                        Description = "description-1"
                    }
                },
                {
                    "recipe-2",
                    new RecipeSummary()
                    {
                        Name = "Sample Recipe 2",
                        TargetService = "Sample Service 2",
                        Id = "recipe-2",
                        ShortDescription = "short-description-2",
                        Description = "description-2"
                    }
                },
                {
                    "recipe-3",
                    new RecipeSummary()
                    {
                        Name = "Sample Recipe 3",
                        TargetService = "Sample Service 3",
                        Id = "recipe-3",
                        ShortDescription = "short-description-3",
                        Description = "description-3"
                    }
                }
            };

        public static List<PublishRecommendation> CreateSampleRecommendations()
        {
            var recommendations = CreateSampleRecommendationSummaries()
                .Select(recommendationSummary => new PublishRecommendation(recommendationSummary))
                .ToList();

            recommendations.First().IsRecommended = true;

            return recommendations;
        }

        public static List<RecommendationSummary> CreateSampleRecommendationSummaries()
        {
            return Enum.GetValues(typeof(DeploymentTypes))
                .OfType<DeploymentTypes>()
                .Select((deploymentType, i) => new RecommendationSummary()
                {
                    RecipeId = $"recipe-{i}",
                    Name = i.ToString(),
                    Description = $"Description for {i}",
                    DeploymentType = deploymentType
                }).ToList();
        }

        public static RecipeSummary GetSampleRecipeSummary(string recipeId)
        {
            return SampleRecipes[recipeId];
        }

        public static List<RepublishTarget> CreateSampleRepublishTargets()
        {
            var existingDeployments = CreateSampleExistingDeployments();

            return existingDeployments
                .Select((x, index) => new RepublishTarget(x) { DeployedToRecently = x.UpdatedByCurrentUser, IsRecommended = index == 0 })
                .ToList();
        }

        public static List<ExistingDeploymentSummary> CreateSampleExistingDeployments()
        {
            var datenow = DateTime.Now;
            return Enumerable.Range(1, 3)
                .Select(i =>
                    {
                        var recipeId = $"recipe-{i}";
                        var recipe = GetSampleRecipeSummary(recipeId);

                        return new ExistingDeploymentSummary()
                        {
                            RecipeId = recipeId,
                            RecipeName = recipe.Name,
                            Name = i.ToString(),
                            TargetService = recipe.TargetService,
                            Description = recipe.Description,
                            ShortDescription = recipe.ShortDescription,
                            LastUpdatedTime = datenow.AddSeconds(i),
                            UpdatedByCurrentUser = i == 1
                        };
                    })
                .OrderByDescending(x => x.UpdatedByCurrentUser)
                .ThenByDescending(x => x.LastUpdatedTime)
                .ToList();
        }
    }
}
