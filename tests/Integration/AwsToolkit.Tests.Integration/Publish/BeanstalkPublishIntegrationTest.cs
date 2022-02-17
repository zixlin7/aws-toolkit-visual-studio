using System.Linq;
using System.Threading;

using Amazon.AWSToolkit.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;

using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    public class BeanstalkPublishIntegrationTest : PublishIntegrationTest
    {
        public BeanstalkPublishIntegrationTest(DeployCliInstallationFixture cliInstallFixture) : base(cliInstallFixture)
        {
            ProjectPath = TestProjects.GetASPNet5();
            StackName = UniqueStackName.CreateWith("BeanstalkTest");
        }

        [Fact]
        public async void ShouldPublish()
        {
            // act
            await SetDeploymentTargetToBeanstalk();

            await DeployToolController.StartDeploymentAsync(SessionId);

            var status = await WaitForDeployment();

            // assert
            Assert.Equal(DeploymentStatus.Success, status);
            await AssertProjectWasDeployed();
        }

        [Fact]
        public async void ShouldValidateSuccessfully()
        {
            // act
            DeleteStackOnCleanup = false;
            await SetDeploymentTargetToBeanstalk();

            var settings = await DeployToolController.GetConfigSettingsAsync(SessionId, CancellationToken.None);

            // Environment Variables were known to fail some CLI versions
            var envVars = Assert.IsType<KeyValueConfigurationDetail>(
                settings.Single(s => s.Id == "ElasticBeanstalkEnvironmentVariables"));

            envVars.KeyValues.Collection.Add(new KeyValue("PATH", "/app"));

            var validation = await DeployToolController.ApplyConfigSettingsAsync(SessionId, settings, CancellationToken.None);

            Assert.False(validation.HasErrors(), "This version of the CLI does not appear to cleanly handle ConfigurationDetail values");
        }
    }
}
