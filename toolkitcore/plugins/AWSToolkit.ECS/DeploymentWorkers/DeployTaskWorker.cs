using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.Util;
using Amazon.CloudWatchLogs;
using Amazon.ECR;
using Amazon.ECS;
using Amazon.ECS.Tools.Commands;
using Amazon.IdentityManagement;
using log4net;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Context;
using Amazon.EC2;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class DeployTaskWorker : BaseWorker, IEcsDeploy
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DeployTaskWorker));

        private readonly IAmazonECR _ecrClient;
        private readonly IAmazonECS _ecsClient;
        private readonly IAmazonCloudWatchLogs _cwlClient;
        private readonly IAmazonEC2 _ec2Client;

        public DeployTaskWorker(IDockerDeploymentHelper helper,
            IAmazonECR ecrClient,
            IAmazonECS ecsClient,
            IAmazonIdentityManagementService iamClient,
            IAmazonCloudWatchLogs cwlClient,
            IAmazonEC2 ec2Client,
            ToolkitContext toolkitContext)
            : base(helper, iamClient, toolkitContext)
        {
            this._ecrClient = ecrClient;
            this._ecsClient = ecsClient;
            this._iamClient = iamClient;
            this._cwlClient = cwlClient;
            this._ec2Client = ec2Client;
        }

        public void Execute(EcsDeployState state)
        {
            Execute(state, this);
        }

        /// <summary>
        /// Overload is for use in tests
        /// </summary>
        public void Execute(EcsDeployState state, IEcsDeploy ecsDeploy)
        {
            Result deployResult = Result.Failed;

            try
            {
                if (ecsDeploy.Deploy(state).Result)
                {
                    deployResult = Result.Succeeded;

                    this.Helper.SendCompleteSuccessAsync(state);
                    if (state.PersistConfigFile.GetValueOrDefault())
                    {
                        base.PersistDeploymentMode(state.HostingWizard);
                    }
                }
                else
                {
                    this.Helper.SendCompleteErrorAsync("ECS Task deployment failed");
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deploying ECS Task.", e);
                this.Helper.SendCompleteErrorAsync("Error deploying ECS Task: " + e.Message);
            }
            finally
            {
                EmitTaskDeploymentMetric(state.Region.Id, deployResult, state.HostingWizard);
            }
        }

        private void EmitTaskDeploymentMetric(string region, Result deployResult, IAWSWizard awsWizard)
        {
            try
            {
                ToolkitContext.TelemetryLogger.RecordEcsDeployTask(new EcsDeployTask()
                {
                    Result = deployResult,
                    EcsLaunchType = EcsTelemetryUtils.GetMetricsEcsLaunchType(awsWizard),
                    AwsRegion = region,
                });
            }
            catch (Exception e)
            {
                Logger.Error("Error logging metric", e);
                Debug.Assert(false, $"Unexpected error while logging deployment metric: {e.Message}");
            }
        }

        async Task<bool> IEcsDeploy.Deploy(EcsDeployState state)
        {
            var credentials =
                ToolkitContext.CredentialManager.GetAwsCredentials(state.Account.Identifier, state.Region);
            var command = new DeployTaskCommand(new ECSToolLogger(this.Helper), state.WorkingDirectory, new string[0])
            {
                Profile = state.Account.Identifier.ProfileName,
                Credentials = credentials,
                Region = state.Region.Id,

                DisableInteractive = true,
                ECRClient = _ecrClient,
                ECSClient = _ecsClient,
                CWLClient = _cwlClient,
                IAMClient = _iamClient,
                EC2Client = _ec2Client,

                PushDockerImageProperties = ConvertToPushDockerImageProperties(state.HostingWizard),
                TaskDefinitionProperties = ConvertToTaskDefinitionProperties(state.HostingWizard),
                DeployTaskProperties = ConvertToDeployTaskProperties(state.HostingWizard),
                ClusterProperties = ConvertToClusterProperties(state.HostingWizard),

                PersistConfigFile = state.PersistConfigFile
            };

            var result = await command.ExecuteAsync();

            if (!result)
            {
                string errorContents = command.LastToolsException?.Message ?? "Unknown";
                string errorMessage = $"Error while deploying ECS Task to AWS: {errorContents}";

                Helper.AppendUploadStatus(errorMessage);
            }

            return result;
        }
    }
}
