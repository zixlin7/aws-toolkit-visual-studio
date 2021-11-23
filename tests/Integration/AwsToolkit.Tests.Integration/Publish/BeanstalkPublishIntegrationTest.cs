using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    public class BeanstalkPublishIntegrationTest : PublishIntegrationTest
    {
        public BeanstalkPublishIntegrationTest()
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
    }
}
