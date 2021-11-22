using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publishing
{
    public class BeanstalkPublishIntegrationTest : PublishIntegrationTest
    {
        public BeanstalkPublishIntegrationTest()
        {
            ProjectPath = TestProjects.GetASPNet5();
            StackName = UniqueStackName.CreateWith("BeanstalkTest");
        }

        //Commented as temporary solution to avoid integration test being run with unit test suite
        //[Fact]
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
