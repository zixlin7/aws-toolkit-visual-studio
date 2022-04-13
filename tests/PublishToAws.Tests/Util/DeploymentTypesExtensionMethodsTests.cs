using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Util;

using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Util
{
    public class DeploymentTypesExtensionMethodsTests
    {
        [Theory]
        [InlineData(DeploymentTypes.BeanstalkEnvironment, DeploymentArtifact.BeanstalkEnvironment)]
        [InlineData(DeploymentTypes.CloudFormationStack, DeploymentArtifact.CloudFormationStack)]
        [InlineData(DeploymentTypes.ElasticContainerRegistryImage, DeploymentArtifact.ElasticContainerRegistry)]
        public void AsDeploymentArtifact(DeploymentTypes deploymentTypes, DeploymentArtifact expectedDeploymentArtifact)
        {
            Assert.Equal(expectedDeploymentArtifact, deploymentTypes.AsDeploymentArtifact());
        }

        [Fact]
        public void AsDeploymentArtifact_UnsupportedTypes()
        {
            // This will fail if the Deploy API introduces new types that the Toolkit does not know about
            Assert.All(GetDeploymentTypes(),
                deploymentTypes => Assert.NotEqual(DeploymentArtifact.Unknown, deploymentTypes.AsDeploymentArtifact()));
        }

        private IEnumerable<DeploymentTypes> GetDeploymentTypes()
        {
            return Enum.GetValues(typeof(DeploymentTypes)).OfType<DeploymentTypes>();
        }
    }
}
