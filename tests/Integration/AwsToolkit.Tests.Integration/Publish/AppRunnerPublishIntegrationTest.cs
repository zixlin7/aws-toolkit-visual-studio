using System.Threading;

using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    public class AppRunnerPublishIntegrationTest : PublishIntegrationTest
    {
        public AppRunnerPublishIntegrationTest(DeployCliInstallationFixture cliInstallFixture) : base(cliInstallFixture)
        {
            ProjectPath = TestProjects.GetASPNet5();
            StackName = UniqueStackName.CreateWith("AppRunnerTest");
        }

        [Fact]
        public async void ShouldPublish()
        {
            // act
            await SetDeploymentTargetToAppRunner();

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
            await SetDeploymentTargetToAppRunner();

            var settings = await DeployToolController.GetConfigSettingsAsync(SessionId, CancellationToken.None);

            var validation = await DeployToolController.ApplyConfigSettingsAsync(SessionId, settings, CancellationToken.None);

            Assert.False(validation.HasErrors(), "This version of the CLI does not appear to cleanly handle ConfigurationDetail values");
        }
    }
}
