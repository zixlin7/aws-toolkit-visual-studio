using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish;
using Amazon.AWSToolkit.Publish.Models;

using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    public class SetDeploymentTargetAsyncTest : PublishIntegrationTest
    {
        public SetDeploymentTargetAsyncTest(DeployCliInstallationFixture cliInstallFixture) : base(cliInstallFixture)
        {
            ProjectPath = TestProjects.GetASPNet5();
            DeleteStackOnCleanup = false;
            StackName = UniqueStackName.CreateWith("SetDeploymentTargetAsyncTest");
        }

        [Fact]
        public async Task ShouldDetectInvalidStackName()
        {
            // setup
            StackName = "some $ invalid stack ! name";

            var deploymentTarget = new PublishRecommendation(new RecommendationSummary()
            {
                RecipeId = "AspNetAppElasticBeanstalkLinux"
            });

            // act
            var exception = await Assert.ThrowsAsync<InvalidApplicationNameException>(async () =>
                await DeployToolController.SetDeploymentTargetAsync(SessionId, deploymentTarget, StackName,
                    CancellationToken.None));

            // assert
            Assert.Contains(InvalidApplicationNameException.ErrorText, exception.Message);
            Assert.Contains(StackName, exception.Message);
        }
    }
}
