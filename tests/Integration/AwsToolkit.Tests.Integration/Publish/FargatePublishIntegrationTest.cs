using System.Threading;

using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    public class FargatePublishIntegrationTest : PublishIntegrationTest
    {
        public FargatePublishIntegrationTest(DeployCliInstallationFixture cliInstallFixture) : base(cliInstallFixture)
        {
            ProjectPath = TestProjects.GetASPNet5();
            StackName = UniqueStackName.CreateWith("FargateTest");
        }

        [Fact]
        public async void ShouldPublish()
        {
            // act
            await SetDeploymentTargetToFargate();

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
            await SetDeploymentTargetToFargate();

            var settings = await DeployToolController.GetConfigSettingsAsync(SessionId, CancellationToken.None);

            var validation = await DeployToolController.ApplyConfigSettingsAsync(SessionId, settings, CancellationToken.None);

            Assert.False(validation.HasErrors(), "This version of the CLI does not appear to cleanly handle ConfigurationDetail values");
        }
    }
}
