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

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class DeployScheduleTaskWorker : BaseWorker, IEcsDeploy
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DeployScheduleTaskWorker));

        private readonly IAmazonCloudWatchEvents _cweClient;
        private readonly IAmazonCloudWatchLogs _cwlClient;
        private readonly IAmazonECR _ecrClient;
        private readonly IAmazonECS _ecsClient;

        public DeployScheduleTaskWorker(IDockerDeploymentHelper helper,
            IAmazonCloudWatchEvents cweClient,
            IAmazonECR ecrClient,
            IAmazonECS ecsClient,
            IAmazonIdentityManagementService iamClient,
            IAmazonCloudWatchLogs cwlClient,
            ToolkitContext toolkitContext)
            : base(helper, iamClient, toolkitContext)
        {
            this._cweClient = cweClient;
            this._ecrClient = ecrClient;
            this._ecsClient = ecsClient;
            this._iamClient = iamClient;
            this._cwlClient = cwlClient;
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
                    this.Helper.SendCompleteErrorAsync("Scheduled ECS Task deployment failed");
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deploying scheduled ECS Task.", e);
                this.Helper.SendCompleteErrorAsync("Error deploying scheduled ECS Task: " + e.Message);
            }
            finally
            {
                EmitTaskDeploymentMetric(state.Region.Id, deployResult, state.HostingWizard);
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

        private void EmitTaskDeploymentMetric(string region, Result deployResult, IAWSWizard awsWizard)
        {
            try
            {
                ToolkitContext.TelemetryLogger.RecordEcsDeployScheduledTask(new EcsDeployScheduledTask()
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
            var command = new DeployScheduledTaskCommand(new ECSToolLogger(this.Helper), state.WorkingDirectory,
                new string[0])
            {
                Profile = state.Account.Identifier.ProfileName,
                Credentials = credentials,
                Region = state.Region.Id,

                DisableInteractive = true,
                CWEClient = this._cweClient,
                ECRClient = this._ecrClient,
                ECSClient = this._ecsClient,
                CWLClient = this._cwlClient,

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
                string errorContents = command.LastToolsException?.Message ?? "Unknown";
                string errorMessage = $"Error while deploying scheduled ECS Task to AWS: {errorContents}";

                Helper.AppendUploadStatus(errorMessage);
            }

            return result;
        }
    }
}
