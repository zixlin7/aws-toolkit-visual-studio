using Amazon.AwsToolkit.Telemetry.Events.Core;
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

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class DeployScheduleTaskWorker : BaseWorker, IEcsDeploy
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DeployScheduleTaskWorker));

        private readonly IAmazonCloudWatchEvents _cweClient;
        private readonly IAmazonCloudWatchLogs _cwlClient;
        private readonly IAmazonECR _ecrClient;
        private readonly IAmazonECS _ecsClient;
        private readonly ITelemetryLogger _telemetryLogger;

        public DeployScheduleTaskWorker(IDockerDeploymentHelper helper,
            IAmazonCloudWatchEvents cweClient,
            IAmazonECR ecrClient,
            IAmazonECS ecsClient,
            IAmazonIdentityManagementService iamClient,
            IAmazonCloudWatchLogs cwlClient)
            : this(helper, cweClient, ecrClient, ecsClient, iamClient, cwlClient,
                ToolkitFactory.Instance.TelemetryLogger)
        {

        }

        public DeployScheduleTaskWorker(IDockerDeploymentHelper helper,
            IAmazonCloudWatchEvents cweClient,
            IAmazonECR ecrClient,
            IAmazonECS ecsClient,
            IAmazonIdentityManagementService iamClient,
            IAmazonCloudWatchLogs cwlClient,
            ITelemetryLogger telemetryLogger)
            : base(helper, iamClient)
        {
            this._cweClient = cweClient;
            this._ecrClient = ecrClient;
            this._ecsClient = ecsClient;
            this._iamClient = iamClient;
            this._cwlClient = cwlClient;
            this._telemetryLogger = telemetryLogger;
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
                EmitTaskDeploymentMetric(state.Region.SystemName, deployResult, state.HostingWizard);
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
                _telemetryLogger.RecordEcsDeployScheduledTask(new EcsDeployScheduledTask()
                {
                    Result = deployResult,
                    EcsLaunchType = EcsTelemetryUtils.GetMetricsEcsLaunchType(awsWizard),
                    RegionId = region,
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
            var command = new DeployScheduledTaskCommand(new ECSToolLogger(this.Helper), state.WorkingDirectory,
                new string[0])
            {
                Profile = state.Account.Name,
                Region = state.Region.SystemName,

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
