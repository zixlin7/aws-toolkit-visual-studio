using System.Collections.Generic;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;
using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.ViewModels
{
    public class MissingCapabilitiesTests
    {
        [Fact]
        public void ShouldUpdateMissingAndResolved()
        {
            var missingCapabilities = new MissingCapabilities();
            var targetCapability = new TargetSystemCapability(new SystemCapabilitySummary() { Name = "Docker" });

            missingCapabilities.Update("recipe-1",
                new List<TargetSystemCapability>() { targetCapability });
            missingCapabilities.Update("recipe-1",
                new List<TargetSystemCapability>());

            Assert.Contains("Docker", missingCapabilities.Missing);
            Assert.Contains("Docker", missingCapabilities.Resolved);
        }

        [Fact]
        public void Update_NullRecipeIdDoesNothing()
        {
            var missingCapabilities = new MissingCapabilities();

            missingCapabilities.Update("", new List<TargetSystemCapability>());

            Assert.Empty(missingCapabilities.Missing);
            Assert.Empty(missingCapabilities.Resolved);
        }
    }
}
