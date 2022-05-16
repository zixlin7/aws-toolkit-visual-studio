using System.Collections.Generic;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;
using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.ViewModels
{
    public class MissingCapabilitiesTests
    {
        private static readonly TargetSystemCapability SampleCapability =
            new TargetSystemCapability(new SystemCapabilitySummary() { Name = "Docker" });

        [Fact]
        public void ShouldUpdateMissingAndResolved()
        {
            var missingCapabilities = new MissingCapabilities();

            missingCapabilities.Update("recipe-1",
                new List<TargetSystemCapability>() { SampleCapability });
            missingCapabilities.Update("recipe-1",
                new List<TargetSystemCapability>());

            Assert.Contains(SampleCapability.Name, missingCapabilities.Missing);
            Assert.Contains(SampleCapability.Name, missingCapabilities.Resolved);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Update_NullRecipeIdDoesNothing(string recipeId)
        {
            var missingCapabilities = new MissingCapabilities();

            missingCapabilities.Update(recipeId,
                new List<TargetSystemCapability>() { SampleCapability });

            Assert.Empty(missingCapabilities.Missing);
            Assert.Empty(missingCapabilities.Resolved);
        }
    }
}
