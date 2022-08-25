using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Models;

using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    public class FargatePublishIntegrationTest : PublishIntegrationTest
    {
        public FargatePublishIntegrationTest(DeployCliInstallationFixture cliInstallFixture) : base(cliInstallFixture)
        {
            ProjectPath = TestProjects.GetASPNet6();
            StackName = UniqueStackName.CreateWith("FargateTest");
        }

        [Fact]
        public async Task ShouldPublish()
        {
            AssertDockerIsRunning();

            // act
            await SetDeploymentTargetToFargate();

            await DeployToolController.StartDeploymentAsync(SessionId);

            var status = await WaitForDeployment();

            // assert
            Assert.Equal(DeploymentStatus.Success, status);
            await AssertDeploymentDetailsAreValid();
            await AssertProjectWasDeployed();
        }

        [Fact]
        public async Task ShouldValidateSuccessfully()
        {
            // act
            DeleteStackOnCleanup = false;
            await SetDeploymentTargetToFargate();

            var settings = await DeployToolController.GetConfigSettingsAsync(SessionId, CancellationToken.None);

            var validation = await DeployToolController.ApplyConfigSettingsAsync(SessionId, settings, CancellationToken.None);

            Assert.False(validation.HasErrors(), "This version of the CLI does not appear to cleanly handle ConfigurationDetail values");
        }

        [Fact]
        public async Task ShouldRetainInvalidValue()
        {
            const string invalidValue = "80zzzz";
            ConfigurationDetail GetDesiredTaskCountDetail(IList<ConfigurationDetail> configurationDetails)
            {
                return configurationDetails.Single(d => d.Id == "DesiredCount");
            }

            // Setup
            DeleteStackOnCleanup = false;
            await SetDeploymentTargetToFargate();

            var settings = await DeployToolController.GetConfigSettingsAsync(SessionId, CancellationToken.None);


            var desiredTaskCount = GetDesiredTaskCountDetail(settings);
            desiredTaskCount.Value = invalidValue; // Set non-int value to int field

            var validation = await DeployToolController.ApplyConfigSettingsAsync(SessionId, desiredTaskCount, CancellationToken.None);
            Assert.True(validation.HasErrors(), "The modified setting should have been flagged as invalid");

            var updatedSettings = await DeployToolController.GetConfigSettingsAsync(SessionId, CancellationToken.None);
            var updatedDesiredTaskCount = GetDesiredTaskCountDetail(updatedSettings);

            // Server returns the "last-valid" and invalid values. We re-apply the invalid value.
            Assert.Equal(invalidValue, updatedDesiredTaskCount.Value);
            Assert.Contains("Value must be", updatedDesiredTaskCount.ValidationMessage);
        }
    }
}
