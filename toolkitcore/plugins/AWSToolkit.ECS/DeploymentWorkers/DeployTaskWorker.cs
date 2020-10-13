﻿using Amazon.AwsToolkit.Telemetry.Events.Core;
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

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class DeployTaskWorker : BaseWorker, IEcsDeploy
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DeployTaskWorker));

        readonly IAmazonECR _ecrClient;
        readonly IAmazonECS _ecsClient;
        readonly IAmazonCloudWatchLogs _cwlClient;
        readonly ITelemetryLogger _telemetryLogger;

        public DeployTaskWorker(IDockerDeploymentHelper helper,
            IAmazonECR ecrClient,
            IAmazonECS ecsClient,
            IAmazonIdentityManagementService iamClient,
            IAmazonCloudWatchLogs cwlClient)
            : this(helper, ecrClient, ecsClient, iamClient, cwlClient, ToolkitFactory.Instance.TelemetryLogger)
        {
        }

        public DeployTaskWorker(IDockerDeploymentHelper helper,
            IAmazonECR ecrClient,
            IAmazonECS ecsClient,
            IAmazonIdentityManagementService iamClient,
            IAmazonCloudWatchLogs cwlClient,
            ITelemetryLogger telemetryLogger)
            : base(helper, iamClient)
        {
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
                EmitTaskDeploymentMetric(state.Region.SystemName, deployResult, state.HostingWizard);
            }
        }

        private void EmitTaskDeploymentMetric(string region, Result deployResult, IAWSWizard awsWizard)
        {
            try
            {
                _telemetryLogger.RecordEcsDeployTask(new EcsDeployTask()
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
            var command = new DeployTaskCommand(new ECSToolLogger(this.Helper), state.WorkingDirectory, new string[0])
            {
                Profile = state.Account.Name,
                Region = state.Region.SystemName,

                DisableInteractive = true,
                ECRClient = this._ecrClient,
                ECSClient = this._ecsClient,
                CWLClient = this._cwlClient,

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