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
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.EC2;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class DeployTaskWorker : BaseWorker, IEcsDeploy
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(DeployTaskWorker));

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
            ActionResults result = null;
          
            void Invoke() => result = DeployTask(state, ecsDeploy);

            void Record(ITelemetryLogger telemetryLogger, double duration)
            {
                var connectionSettings = new AwsConnectionSettings(state.Account?.Identifier, state.Region);
                EmitTaskDeploymentMetric(connectionSettings, result, state.HostingWizard, duration);
            }

            ToolkitContext.TelemetryLogger.TimeAndRecord(Invoke, Record);
        }

        private ActionResults DeployTask(EcsDeployState state, IEcsDeploy ecsDeploy)
        {
            try
            {
                var result = ecsDeploy.Deploy(state).Result;
                if (result.Success)
                {
                    Helper.SendCompleteSuccessAsync(state);
                    if (state.PersistConfigFile.GetValueOrDefault())
                    {
                        base.PersistDeploymentMode(state.HostingWizard);
                    }
                }
                else
                {
                    Helper.SendCompleteErrorAsync("ECS Task deployment failed");
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.Error("Error deploying ECS Task.", e);
                Helper.SendCompleteErrorAsync("Error deploying ECS Task: " + e.Message);
                return ActionResults.CreateFailed(e);
            }
        }

        private void EmitTaskDeploymentMetric(AwsConnectionSettings connectionSettings, ActionResults result, IAWSWizard awsWizard, double duration)
        {
            try
            {
                ToolkitContext.TelemetryLogger.RecordEcsDeployTask(new EcsDeployTask()
                {
                    Result = result.AsTelemetryResult(),
                    EcsLaunchType = EcsTelemetryUtils.GetMetricsEcsLaunchType(awsWizard),
                    AwsRegion = connectionSettings.Region?.Id ?? MetadataValue.Invalid,
                    AwsAccount = connectionSettings.GetAccountId(ToolkitContext.ServiceClientManager) ?? MetadataValue.Invalid,
                    Reason = EcsTelemetryUtils.GetReason(result?.Exception),
                    Duration = duration
                });
            }
            catch (Exception e)
            {
                _logger.Error("Error logging metric", e);
                Debug.Assert(false, $"Unexpected error while logging deployment metric: {e.Message}");
            }
        }

        async Task<ActionResults> IEcsDeploy.Deploy(EcsDeployState state)
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

            var exception = DetermineErrorException(command.LastToolsException, "Failed to deploy ECS task to AWS");
            return result ? new ActionResults().WithSuccess(true) : ActionResults.CreateFailed(exception);
        }
    }
}
