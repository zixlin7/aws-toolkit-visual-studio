using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Install;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Publish.Services;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    public abstract class PublishIntegrationTest : IAsyncLifetime
    {
        protected IDeployToolController DeployToolController;
        protected ConfigurationDetailFactory ConfigurationDetailFactory;
        protected string SessionId;

        protected string StackName;
        protected string ProjectPath;

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
            var installOptions = InstallOptionsFactory.Create(new ToolkitHostInfo("defaultName", "2022"));
            return CliServerFactory.CreateAsync(installOptions, new FilePublishSettingsRepository());
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
            await CloudFormationStacks.DeleteStack(StackName);
        }

        protected async Task SetDeploymentTargetToBeanstalk()
        {
            await DeployToolController.SetDeploymentTarget(SessionId, StackName,
                "AspNetAppElasticBeanstalkLinux", false, CancellationToken.None);
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
