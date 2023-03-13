using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Services;
using Amazon.AWSToolkit.Publish.Views;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.VersionInfo;

using log4net;

using Microsoft;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.Publish.Package
{
    /// <summary>
    /// VS Service used to manage the Publish to AWS experience.
    /// </summary>
    public class PublishToAws : IPublishToAws, SPublishToAws
    {
        internal enum ShowDialogResult
        {
            Success,
            Fail,
            Cancel,
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(PublishToAws));
        private const int MinimumSupportedDotNetVersion = 6;

        private readonly PublishContext _publishContext;
        private readonly ProgressStepProcessor _showDocumentStepProcessor = new PublishDialogStepProcessor();

        public PublishToAws(PublishContext publishContext)
        {
            _publishContext = publishContext;
        }

        public Task ShowPublishToAwsDocument(ShowPublishToAwsDocumentArgs args) =>
            ShowPublishToAwsDocumentAsync(args, new DotNetVersionProvider());

        public async Task ShowPublishToAwsDocumentAsync(ShowPublishToAwsDocumentArgs args, IDotNetVersionProvider dotNetVersionProvider)
        {
            try
            {
                var steps = new List<ShowPublishDialogStep>();

#if VS2019
                // VS 2019 only supports up to .NET Core 3.1, which reached End of Life in December 2022.
                // Attempt to check for .NET 6 (or newer) before proceeding.
                // Newer VS versions do not need this check, since their installed .NET version is supported.
                steps.Add(new ShowPublishDialogStep(async (cancellationToken) => await CheckDotNetVersionAsync(dotNetVersionProvider, cancellationToken),
                    "Checking .NET Version...", true));
#endif

                steps.Add(new ShowPublishDialogStep(async (cancellationToken) => await InstallDeployToolAsync(),
                    "Installing Deploy Tool...", true));

                ICliServer cliServer = null;
                steps.Add(new ShowPublishDialogStep(async (cancellationToken) =>
                {
                    cliServer = await InitializeAsync(args);
                }, "Initializing Deploy Tool...", true));

                steps.Add(new ShowPublishDialogStep(async (cancellationToken) => await ShowDocumentTabAsync(args, cliServer),
                    "Starting up...", false));

                var result = await ShowPublishToAwsDocumentAsync(args, steps).ConfigureAwait(false);
                RecordPublishStartMetric(args.AccountId, args.Region, args.Requester, AsResult(result));
            }
            catch (Exception e)
            {
                RecordPublishStartMetric(args.AccountId, args.Region, args.Requester, Result.Failed);

                Logger.Error("Failed to open the Publish document", e);

                _publishContext.ToolkitShellProvider.OutputToHostConsole(
                    $"Unable to open the Publish to AWS dialog: {e.Message}",
                    true);

                ShowStartupError(e.Message);
            }
        }

        private Task CheckDotNetVersionAsync(IDotNetVersionProvider versionProvider, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dotNetVersion = versionProvider.GetMajorVersion();

            if (dotNetVersion.HasValue && dotNetVersion.Value >= MinimumSupportedDotNetVersion)
            {
                return Task.CompletedTask;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var title = $"Publish to AWS requires .NET {MinimumSupportedDotNetVersion} or greater";

            if (dotNetVersion.HasValue)
            {
                // User has an older .NET installed. They need to install a newer version.
                _publishContext.ToolkitShellProvider.ShowError(
                    title,
                    $".NET {MinimumSupportedDotNetVersion} is required for Publish to AWS, .NET {dotNetVersion} was detected." +
                    Environment.NewLine + Environment.NewLine +
                    "Install a newer version of .NET, and try again.");

                throw new OperationCanceledException($"Unsupported .NET version: {dotNetVersion}");
            }
            else
            {
                // Possible scenarios:
                // - The Toolkit wasn't able to find .NET installed
                // - The user doesn't have .NET installed
                // - Something changed with the way the dotnet CLI returns the version, which is not compatible with how the Toolkit is obtaining it
                // Allow the user to proceed, but log the details in case they have a bad experience.

                // Maybe the Toolkit wasn't able to find it, let the user try to proceed
                var message = "AWS Toolkit could not find a .NET installation. Publish to AWS may not work properly." +
                              $" You may need to install .NET {MinimumSupportedDotNetVersion} or newer.";

                Logger.Warn(message);
                _publishContext.ToolkitShellProvider.OutputToHostConsole(message, true);
            }

            return Task.CompletedTask;
        }

        private async Task InstallDeployToolAsync()
        {
            await WaitForCliInitializationToCompleteAsync();
        }

        private async Task WaitForCliInitializationToCompleteAsync()
        {
            await _publishContext.InitializeCliTask;
        }

        private async Task<ICliServer> InitializeAsync(ShowPublishToAwsDocumentArgs showPublishToAwsDocumentArgs)
        {
            ICliServer cliServer = null;

            async Task Initialize()
            {
                cliServer = await InitializeDeployToolAsync();
            }

            void Record(ITelemetryLogger telemetryLogger, long milliseconds)
            {
                var result = cliServer == null ? Result.Failed : Result.Succeeded;
                RecordInitialized(telemetryLogger, result, showPublishToAwsDocumentArgs, milliseconds);
            }

            await _publishContext.ToolkitContext.TelemetryLogger.TimeAndRecordAsync(Initialize, Record);
            return cliServer;
        }

        private async Task<ICliServer> InitializeDeployToolAsync()
        {
            try
            {
                ICliServer cliServer = await _publishContext.PublishPackage.GetServiceAsync(typeof(SCliServer)) as ICliServer;
                Assumes.Present(cliServer);
                await cliServer.StartAsync(_publishContext.PublishPackage.DisposalToken);
                return cliServer;
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to start deploy tooling: {e.Message}", e);
            }
        }

        private async Task ShowDocumentTabAsync(ShowPublishToAwsDocumentArgs args, ICliServer cliServer)
        {
            bool success = false;

            async Task Show()
            {
                await CreateAndShowDocumentAsync(args, cliServer);
                success = true;
            }

            void Record(ITelemetryLogger telemetryLogger, long milliseconds)
            {
                var result = success ? Result.Succeeded : Result.Failed;
                RecordShow(telemetryLogger, result, args, milliseconds);
            }

            await _publishContext.ToolkitContext.TelemetryLogger.TimeAndRecordAsync(Show, Record);
        }

        private async Task CreateAndShowDocumentAsync(ShowPublishToAwsDocumentArgs args, ICliServer cliServer)
        {
            try
            {
                await _publishContext.PublishPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                var publishApplicationContext = new PublishApplicationContext(_publishContext);

                IAWSToolkitControl publishDocument =
                    new PublishToAwsDocumentControl(args, publishApplicationContext, cliServer);
                await _publishContext.ToolkitShellProvider.OpenInEditorAsync(publishDocument);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to create the deployment screen: {e.Message}", e);
            }
        }

        private async Task<ShowDialogResult> ShowPublishToAwsDocumentAsync(ShowPublishToAwsDocumentArgs args,
            List<ShowPublishDialogStep> steps)
        {
            ShowDialogResult showResult = ShowDialogResult.Fail;

            async Task Show()
            {
                showResult = await ShowPublishToAwsDocumentAsync(steps);
            }

            void Record(ITelemetryLogger telemetryLogger, long milliseconds)
            {
                RecordFullPublishSetup(telemetryLogger, AsResult(showResult), args, milliseconds);
            }

            await _publishContext.ToolkitContext.TelemetryLogger.TimeAndRecordAsync(Show, Record);
            return showResult;
        }

        private async Task<ShowDialogResult> ShowPublishToAwsDocumentAsync(List<ShowPublishDialogStep> steps)
        {
            var progressDialog = await _publishContext.ToolkitShellProvider.CreateProgressDialog();
            try
            {
                progressDialog.Caption = "Publish to AWS";
                progressDialog.TotalSteps = steps.Count + 1;
                progressDialog.Show(1);

                await _showDocumentStepProcessor.ProcessStepsAsync(progressDialog, steps);
                return ShowDialogResult.Success;
            }
            catch (OperationCanceledException)
            {
                // Swallow if the user cancelled. We've broken out of the steps above.
                _publishContext.ToolkitShellProvider.OutputToHostConsole(
                    $"Launching the Publish to AWS dialog has been cancelled",
                    true);

                return ShowDialogResult.Cancel;
            }
            finally
            {
                progressDialog?.Hide();
                progressDialog?.Dispose();
            }
        }

        private void ShowStartupError(string errorMessage)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("There was a problem trying to open the Publish dialog.");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine(errorMessage);

            _publishContext.ToolkitShellProvider.ShowMessage("Unable to Publish to AWS",
                messageBuilder.ToString());
        }

        private void RecordPublishStartMetric(string accountId, ToolkitRegion region, string operationOrigin, Result result)
        {
            _publishContext.ToolkitContext.TelemetryLogger.RecordPublishStart(new PublishStart()
            {
                AwsAccount = accountId,
                AwsRegion = region?.Id ?? MetadataValue.NotSet,
                Source = operationOrigin,
                Result = result,
            });
        }

        private void RecordInitialized(ITelemetryLogger telemetryLogger, Result result,
            ShowPublishToAwsDocumentArgs showPublishToAwsDocumentArgs, long milliseconds)
        {
            telemetryLogger.RecordPublishSetup(new PublishSetup()
            {
                PublishSetupStage = PublishSetupStage.Initialize,
                Result = result,
                Value = milliseconds,
                Duration = milliseconds,
                AwsAccount = showPublishToAwsDocumentArgs.AccountId,
                AwsRegion = showPublishToAwsDocumentArgs.Region?.Id ?? MetadataValue.NotSet,
            });
        }

        private void RecordShow(ITelemetryLogger telemetryLogger, Result result, ShowPublishToAwsDocumentArgs args, long milliseconds)
        {
            telemetryLogger.RecordPublishSetup(new PublishSetup()
            {
                PublishSetupStage = PublishSetupStage.Show,
                Result = result,
                Value = milliseconds,
                Duration = milliseconds,
                AwsAccount = args.AccountId,
                AwsRegion = args.Region?.Id ?? MetadataValue.NotSet,
            });
        }

        private void RecordFullPublishSetup(ITelemetryLogger telemetryLogger, Result result, ShowPublishToAwsDocumentArgs args, long milliseconds)
        {
            telemetryLogger.RecordPublishSetup(new PublishSetup()
            {
                PublishSetupStage = PublishSetupStage.All,
                Result = result,
                Passive = false,
                Value = milliseconds,
                Duration = milliseconds,
                AwsAccount = args.AccountId,
                AwsRegion = args.Region?.Id ?? MetadataValue.NotSet,
            });
        }

        private static Result AsResult(ShowDialogResult showResult)
        {
            switch (showResult)
            {
                case ShowDialogResult.Success:
                    return Result.Succeeded;
                case ShowDialogResult.Cancel:
                    return Result.Cancelled;
                default:
                    return Result.Failed;
            }
        }
    }
}
