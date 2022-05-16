using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Publish.Util;
using Amazon.AWSToolkit.Telemetry;

using log4net;

namespace Amazon.AWSToolkit.Publish.Install
{
    /// <summary>
    /// Installs and verifies the NuGet package aws.deploy.tools
    /// </summary>
    public class DeployCli
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DeployCli));

        private readonly ToolkitContext _toolkitContext;
        private readonly InstallOptions _options;

        public DeployCli(InstallOptions options, ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            _options = options;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await InstallCliAsync(cancellationToken);
            await VerifyCliAsync(cancellationToken);
            Logger.Debug($"Finished initializing {PublishToAwsConstants.DeployToolPackageName}");
        }

        private async Task InstallCliAsync(CancellationToken cancellationToken)
        {
            bool success = false;
            InstallResult installResult = InstallResult.Installed;

            async Task Install()
            {
                installResult = await InstallDeployCliAsync(cancellationToken);
                success = true;
            }

            void Record(ITelemetryLogger telemetryLogger, long milliseconds)
            {
                RecordCliInstalled(telemetryLogger, AsResult(success, cancellationToken),
                    AsInstallMode(installResult), milliseconds);
            }

            await _toolkitContext.TelemetryLogger.TimeAndRecord(Install, Record);
        }

        private async Task<InstallResult> InstallDeployCliAsync(CancellationToken cancellationToken)
        {
            var installer = DeployCliInstallerFactory.Create(_options);
            return await installer.InstallAsync(cancellationToken);
        }

        private async Task VerifyCliAsync(CancellationToken cancellationToken)
        {
            bool success = false;

            async Task Verify()
            {
                await VerifyDeployCliAsync(cancellationToken);
                success = true;
            }

            void Record(ITelemetryLogger telemetryLogger, long milliseconds)
            {
                RecordCliValidated(telemetryLogger, AsResult(success, cancellationToken), milliseconds);
            }

            await _toolkitContext.TelemetryLogger.TimeAndRecord(Verify, Record);
        }

        private async Task VerifyDeployCliAsync(CancellationToken cancellationToken)
        {
            var verifyDeployCli = new VerifyDeployCli(_options);
            await verifyDeployCli.ExecuteAsync(cancellationToken);
        }

        private static Result AsResult(bool success, CancellationToken cancellationToken)
        {
            return success ? Result.Succeeded : cancellationToken.IsCancellationRequested ? Result.Cancelled : Result.Failed;
        }

        private static PublishInstallCliMode AsInstallMode(InstallResult installResult)
        {
            switch (installResult)
            {
                case InstallResult.Skipped:
                    return PublishInstallCliMode.Noop;
                case InstallResult.Updated:
                    return PublishInstallCliMode.Upgrade;
                default:
                    return PublishInstallCliMode.Install;
            }
        }

        private void RecordCliInstalled(ITelemetryLogger telemetryLogger, Result result,
            PublishInstallCliMode installMode, long milliseconds)
        {
            telemetryLogger.RecordPublishSetup(new PublishSetup()
            {
                PublishSetupStage = PublishSetupStage.Install,
                PublishInstallCliMode = installMode,
                Result = result,
                Value = milliseconds,
                Duration = milliseconds,
                AwsAccount = GetAccountId(),
                AwsRegion = MetadataValue.NotApplicable,
            });
        }

        private void RecordCliValidated(ITelemetryLogger telemetryLogger, Result result, long milliseconds)
        {
            telemetryLogger.RecordPublishSetup(new PublishSetup()
            {
                PublishSetupStage = PublishSetupStage.Validate,
                Result = result,
                Value = milliseconds,
                Duration = milliseconds,
                AwsAccount = GetAccountId(),
                AwsRegion = MetadataValue.NotApplicable,
            });
        }

        private string GetAccountId()
        {
            return _toolkitContext.ConnectionManager.ActiveAccountId ?? MetadataValue.NotSet;
        }
    }
}
