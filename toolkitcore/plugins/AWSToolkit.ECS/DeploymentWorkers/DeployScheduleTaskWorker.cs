using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.Util;
using Amazon.CloudWatchEvents;
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
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class DeployScheduleTaskWorker : BaseWorker, IEcsDeploy
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(DeployScheduleTaskWorker));

        private readonly IAmazonCloudWatchEvents _cweClient;
        private readonly IAmazonCloudWatchLogs _cwlClient;
        private readonly IAmazonECR _ecrClient;
        private readonly IAmazonECS _ecsClient;
        private readonly IAmazonEC2 _ec2Client;

        public DeployScheduleTaskWorker(IDockerDeploymentHelper helper,
            IAmazonCloudWatchEvents cweClient,
            IAmazonECR ecrClient,
            IAmazonECS ecsClient,
            IAmazonIdentityManagementService iamClient,
            IAmazonCloudWatchLogs cwlClient,
            IAmazonEC2 ec2Client,
            ToolkitContext toolkitContext)
            : base(helper, iamClient, toolkitContext)
        {
            this._cweClient = cweClient;
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
                    Helper.SendCompleteErrorAsync("Scheduled ECS Task deployment failed");
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.Error("Error deploying scheduled ECS Task.", e);
                Helper.SendCompleteErrorAsync("Error deploying scheduled ECS Task: " + e.Message);
                return ActionResults.CreateFailed(e);
            }
        }

        private static bool CanCreateCloudWatchEventRole(EcsDeployState state)
        {
            return state.HostingWizard[PublishContainerToAWSWizardProperties.CreateCloudWatchEventIAMRole] is bool &&
                   (bool)state.HostingWizard[PublishContainerToAWSWizardProperties.CreateCloudWatchEventIAMRole];
        }

        private void CreateCloudWatchEventRole(DeployScheduledTaskCommand command)
        {
            var roleName =
                Amazon.Common.DotNetCli.Tools.RoleHelper.GenerateUniqueIAMRoleName(this._iamClient,
                    "ecsEventsRole");
            this.Helper.AppendUploadStatus("Creating IAM role " + roleName +
                                           " for CloudWatch Events to use to run the ECS task");
            var roleArn = Amazon.Common.DotNetCli.Tools.RoleHelper.CreateRole(this._iamClient, roleName,
                Amazon.Common.DotNetCli.Tools.Constants.CWE_ASSUME_ROLE_POLICY,
                "AmazonEC2ContainerServiceEventsRole");

            command.DeployScheduledTaskProperties.CloudWatchEventIAMRole = roleArn;
        }

        private void EmitTaskDeploymentMetric(AwsConnectionSettings connectionSettings, ActionResults result, IAWSWizard awsWizard, double duration)
        {
            try
            {
                var data = result.CreateMetricData<EcsDeployScheduledTask>(connectionSettings,
                    ToolkitContext.ServiceClientManager);
                data.Result = result.AsTelemetryResult();
                data.EcsLaunchType = EcsTelemetryUtils.GetMetricsEcsLaunchType(awsWizard);
                data.Duration = duration;
                ToolkitContext.TelemetryLogger.RecordEcsDeployScheduledTask(data);
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
            var command = new DeployScheduledTaskCommand(new ECSToolLogger(this.Helper), state.WorkingDirectory,
                new string[0])
            {
                Profile = state.Account.Identifier.ProfileName,
                Credentials = credentials,
                Region = state.Region.Id,

                DisableInteractive = true,
                CWEClient = _cweClient,
                ECRClient = _ecrClient,
                ECSClient = _ecsClient,
                CWLClient = _cwlClient,
                IAMClient = _iamClient,
                EC2Client = _ec2Client,

                PushDockerImageProperties = ConvertToPushDockerImageProperties(state.HostingWizard),
                TaskDefinitionProperties = ConvertToTaskDefinitionProperties(state.HostingWizard),
                DeployScheduledTaskProperties = ConvertToDeployScheduledTaskProperties(state.HostingWizard),
                ClusterProperties = ConvertToClusterProperties(state.HostingWizard),

                PersistConfigFile = state.PersistConfigFile
            };

            if (CanCreateCloudWatchEventRole(state))
            {
                CreateCloudWatchEventRole(command);
            }

            var result = await command.ExecuteAsync();

            if (!result)
            {
                string errorContents = command.LastException?.Message ?? "Unknown";
                string errorMessage = $"Error while deploying scheduled ECS Task to AWS: {errorContents}";

                Helper.AppendUploadStatus(errorMessage);
            }

            var exception = DetermineErrorException(command.LastException, "Failed to deploy scheduled ECS task to AWS");
            return result ? new ActionResults().WithSuccess(true) : ActionResults.CreateFailed(exception);
        }
    }
}
