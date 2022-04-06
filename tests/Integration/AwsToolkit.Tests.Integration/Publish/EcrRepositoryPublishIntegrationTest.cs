using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.ECR;
using Amazon.ECR.Model;

using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    public class EcrRepositoryPublishIntegrationTest : PublishIntegrationTest
    {
        private readonly string _repoName;
        private bool _deleteRepoOnCleanup = true;

        public EcrRepositoryPublishIntegrationTest(DeployCliInstallationFixture cliInstallFixture) : base(cliInstallFixture)
        {
            ProjectPath = TestProjects.GetASPNet5();
            StackName = UniqueStackName.CreateWith("EcrRepoTest");
            _repoName = StackName.ToLower();
        }

        public override async Task DisposeAsync()
        {
            if (_deleteRepoOnCleanup)
            {
                await DeleteRepoAsync(_repoName);
            }

            await base.DisposeAsync();
        }

        [Fact]
        public async Task ShouldPublish()
        {
            // setup
            DeleteStackOnCleanup = false;

            // act
            await SetDeploymentTargetToEcrRepo();

            await DeployToolController.StartDeploymentAsync(SessionId);

            var status = await WaitForDeployment();

            // assert
            Assert.Equal(DeploymentStatus.Success, status);
            Assert.True(await RepoExistsAsync(_repoName));
        }

        [Fact]
        public async Task ShouldValidateSuccessfully()
        {
            // act
            _deleteRepoOnCleanup = false;
            DeleteStackOnCleanup = false;
            await SetDeploymentTargetToEcrRepo();

            var settings = await DeployToolController.GetConfigSettingsAsync(SessionId, CancellationToken.None);

            var validation = await DeployToolController.ApplyConfigSettingsAsync(SessionId, settings, CancellationToken.None);

            Assert.False(validation.HasErrors(), "This version of the CLI does not appear to cleanly handle ConfigurationDetail values");
        }

        private async Task<bool> RepoExistsAsync(string repoName)
        {
            var client = await GetEcrClient();

            var response = await client.DescribeRepositoriesAsync(new DescribeRepositoriesRequest()
            {
                RepositoryNames = new List<string>() { repoName },
            });

            return response.Repositories.Any();
        }

        private async Task DeleteRepoAsync(string repoName)
        {
            var client = await GetEcrClient();

            await client.DeleteRepositoryAsync(new DeleteRepositoryRequest()
            {
                RepositoryName = repoName,
                Force = true,
            });
        }

        private async Task<AmazonECRClient> GetEcrClient()
        {
            var credentials = await GetCredentials();
            var client = new AmazonECRClient(credentials, RegionEndpoint.GetBySystemName(RegionId));
            return client;
        }
    }
}
