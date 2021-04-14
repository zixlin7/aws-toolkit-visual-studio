using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.ECR;
using Amazon.ECS.Tools.Commands;
using log4net;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Context;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class PushImageToECRWorker : BaseWorker, IEcsDeploy
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DeployTaskWorker));

        readonly IAmazonECR _ecrClient;

        public PushImageToECRWorker(IDockerDeploymentHelper helper, IAmazonECR ecrClient,
            ToolkitContext toolkitContext)
            : base(helper, null, toolkitContext)
        {
            this._ecrClient = ecrClient;
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

                    this.Helper.SendImagePushCompleteSuccessAsync(state);
                    if (state.PersistConfigFile.GetValueOrDefault())
                    {
                        base.PersistDeploymentMode(state.HostingWizard);
                    }
                }
                else
                {
                    this.Helper.SendCompleteErrorAsync("ECR image publish failed");
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error pushing to ECR repository.", e);
                this.Helper.SendCompleteErrorAsync("Error pushing to ECR repository: " + e.Message);
            }
            finally
            {
                EmitImageDeploymentMetric(state.Region.Id, deployResult);
            }
        }

        private void EmitImageDeploymentMetric(string region, Result deployResult)
        {
            try
            {
                ToolkitContext.TelemetryLogger.RecordEcrDeployImage(new EcrDeployImage()
                {
                    Result = deployResult,
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
            var credentials =
                ToolkitContext.CredentialManager.GetAwsCredentials(state.Account.Identifier, state.Region);
            var command = new PushDockerImageCommand(new ECSToolLogger(this.Helper), state.WorkingDirectory, new string[0])
            {
                Profile = state.Account.Identifier.ProfileName,
                Credentials = credentials,
                Region = state.Region.Id,

                DisableInteractive = true,
                ECRClient = this._ecrClient,

                PushDockerImageProperties = ConvertToPushDockerImageProperties(state.HostingWizard),

                PersistConfigFile = state.PersistConfigFile
            };

            var result = await command.ExecuteAsync();

            if (!result)
            {
                string errorContents = command.LastToolsException?.Message ?? "Unknown";
                string errorMessage = $"Error publishing container to AWS: {errorContents}";

                Helper.AppendUploadStatus(errorMessage);
            }

            return result;
        }
    }
}
