using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Publish.Services;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    [Collection(InstalledCliTestCollection.Name)]
    public abstract class PublishIntegrationTest : IAsyncLifetime, IClassFixture<DeployCliInstallationFixture>
    {
        private readonly DeployCliInstallationFixture _cliInstallFixture;
        private readonly IAWSToolkitShellProvider _toolkitHost = new NoOpToolkitShellProvider();

        protected IDeployToolController DeployToolController;
        protected ConfigurationDetailFactory ConfigurationDetailFactory;
        protected string SessionId;

        protected string StackName;
        protected bool DeleteStackOnCleanup = true;
        protected string ProjectPath;

        protected PublishIntegrationTest(DeployCliInstallationFixture cliInstallFixture)
        {
            _cliInstallFixture = cliInstallFixture;
        }

        public async Task InitializeAsync()
        {
            ConfigurationDetailFactory = new ConfigurationDetailFactory(null, null);
            DeployToolController = await CreateDeployToolController();
            await CreateSessionAndSetId();
        }

        protected async Task<DeployToolController> CreateDeployToolController()
        {
            var restClient = await CreateRestClient();
            return new DeployToolController(restClient, ConfigurationDetailFactory);
        }

        protected async Task<IRestAPIClient> CreateRestClient()
        {
            ICliServer cliServer = await CreateCliServer();
            await cliServer.StartAsync(CancellationToken.None);
            return cliServer.GetRestClient(GetCredentials);
        }

        protected Task<CliServer> CreateCliServer()
        {
            return CliServerFactory.CreateAsync(_cliInstallFixture.InstallOptions, new FilePublishSettingsRepository(),
                _toolkitHost);
        }

        protected Task<AWSCredentials> GetCredentials()
        {
            var chain = new CredentialProfileStoreChain();
            chain.TryGetAWSCredentials("default", out var awsCredentials);
            return Task.FromResult(awsCredentials);
        }

        private async Task CreateSessionAndSetId()
        {
            var result = await DeployToolController.StartSessionAsync("us-west-2", ProjectPath, CancellationToken.None);
            SessionId = result.SessionId;
        }

        public async Task DisposeAsync()
        {
            if (DeleteStackOnCleanup)
            {
                await CloudFormationStacks.DeleteStack(StackName);
            }
        }

        protected async Task SetDeploymentTargetToBeanstalk()
        {
            var deploymentTarget = new PublishRecommendation(new RecommendationSummary()
            {
                RecipeId = "AspNetAppElasticBeanstalkLinux"
            });

            await DeployToolController.SetDeploymentTargetAsync(SessionId, deploymentTarget, StackName, CancellationToken.None);
        }

        protected async Task SetDeploymentTargetToAppRunner()
        {
            var deploymentTarget = new PublishRecommendation(new RecommendationSummary()
            {
                RecipeId = "AspNetAppAppRunner"
            });

            await DeployToolController.SetDeploymentTargetAsync(SessionId, deploymentTarget, StackName, CancellationToken.None);
        }

        protected async Task SetDeploymentTargetToFargate()
        {
            var deploymentTarget = new PublishRecommendation(new RecommendationSummary()
            {
                RecipeId = "AspNetAppEcsFargate"
            });

            await DeployToolController.SetDeploymentTargetAsync(SessionId, deploymentTarget, StackName, CancellationToken.None);
        }

        protected async Task<DeploymentStatus> WaitForDeployment()
        {
            DeploymentStatus status = DeploymentStatus.Error;

            await WaitUntil(async () =>
            {
                status = (await DeployToolController.GetDeploymentStatusAsync(SessionId)).Status;
                return status != DeploymentStatus.Executing;
            }, TimeSpan.FromSeconds(1));

            return status;
        }

        private async Task WaitUntil(Func<Task<bool>> predicate, TimeSpan frequency)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!await predicate())
                {
                    await Task.Delay(frequency);
                }
            });
            await waitTask;
        }

        protected async Task AssertProjectWasDeployed()
        {
            await ResetConnection();
            Assert.True(await IsProjectDeployed());
        }

        protected async Task ResetConnection()
        {
            await CancelSession();
            await CreateSessionAndSetId();
        }

        protected async Task CancelSession()
        {
            await DeployToolController.StopSessionAsync(SessionId, CancellationToken.None);
        }

        private async Task<bool> IsProjectDeployed()
        {
            var republishTargets = await DeployToolController.GetRepublishTargetsAsync(SessionId, ProjectPath, CancellationToken.None);
            return republishTargets.Any(republishTarget => republishTarget.Name == StackName);
        }
    }
}
