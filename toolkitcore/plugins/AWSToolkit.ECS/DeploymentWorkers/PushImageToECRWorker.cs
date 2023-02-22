using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.ECR;
using Amazon.ECS.Tools.Commands;
using log4net;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ECS.Util;
using Amazon.AWSToolkit.Navigator;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class PushImageToECRWorker : BaseWorker, IEcsDeploy
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(PushImageToECRWorker));

        private readonly IAmazonECR _ecrClient;

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
            ActionResults result = null;

            void Invoke() => result = PushImage(state, ecsDeploy);

            void Record(ITelemetryLogger telemetryLogger, double duration)
            {
                var connectionSettings = new AwsConnectionSettings(state.Account?.Identifier, state.Region);
                EmitImageDeploymentMetric(connectionSettings, result, duration);
            }

            ToolkitContext.TelemetryLogger.TimeAndRecord(Invoke, Record);
        }

        private ActionResults PushImage(EcsDeployState state, IEcsDeploy ecsDeploy)
        {
            try
            {
                var result = ecsDeploy.Deploy(state).Result;
                if (result.Success)
                {
                    Helper.SendImagePushCompleteSuccessAsync(state);
                    if (state.PersistConfigFile.GetValueOrDefault())
                    {
                        base.PersistDeploymentMode(state.HostingWizard);
                    }
                }
                else
                {
                    Helper.SendCompleteErrorAsync("ECR image publish failed");
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.Error("Error pushing to ECR repository.", e);
                Helper.SendCompleteErrorAsync("Error pushing to ECR repository: " + e.Message);
                return ActionResults.CreateFailed(e);
            }
        }

        private void EmitImageDeploymentMetric(AwsConnectionSettings connectionSettings, ActionResults result, double duration)
        {
            try
            {
                ToolkitContext.TelemetryLogger.RecordEcrDeployImage(new EcrDeployImage()
                {
                    Result = result.AsTelemetryResult(),
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

            var exception = DetermineErrorException(command.LastToolsException, "Failed to push image to AWS");
            return result ? new ActionResults().WithSuccess(true) : ActionResults.CreateFailed(exception);
        }
    }
}
