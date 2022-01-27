using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Services;
using Amazon.AWSToolkit.Publish.Views;
using Amazon.AWSToolkit.Shared;

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
        static readonly ILog Logger = LogManager.GetLogger(typeof(PublishToAws));

        private readonly PublishContext _publishContext;
        private readonly ProgressStepProcessor _showDocumentStepProcessor = new PublishDialogStepProcessor();

        public PublishToAws(PublishContext publishContext)
        {
            _publishContext = publishContext;
        }

        public async Task ShowPublishToAwsDocument(ShowPublishToAwsDocumentArgs args)
        {
            try
            {
                var steps = new List<ShowPublishDialogStep>();

                steps.Add(new ShowPublishDialogStep(async () => await InstallDeployToolAsync(),
                    "Installing Deploy Tool...", true));

                ICliServer cliServer = null;
                steps.Add(new ShowPublishDialogStep(async () =>
                {
                    cliServer = await InitializeDeployToolAsync();
                }, "Initializing Deploy Tool...", true));

                steps.Add(new ShowPublishDialogStep(async () => await CreateAndShowDocumentAsync(args, cliServer),
                    "Starting up...", false));

                await ShowPublishToAwsDocumentAsync(steps).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to open the Publish document", e);

                _publishContext.ToolkitShellProvider.OutputToHostConsole(
                    $"Unable to open the Publish to AWS dialog: {e.Message}",
                    true);

                ShowStartupError(e.Message);
            }
        }

        private async Task InstallDeployToolAsync()
        {
            await WaitForCliInitializationToCompleteAsync();
        }

        private async Task WaitForCliInitializationToCompleteAsync()
        {
            await _publishContext.InitializeCliTask;
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

        private async Task ShowPublishToAwsDocumentAsync(List<ShowPublishDialogStep> steps)
        {
            var progressDialog = await _publishContext.ToolkitShellProvider.CreateProgressDialog();
            try
            {
                progressDialog.Caption = "Publish to AWS";
                progressDialog.TotalSteps = steps.Count + 1;
                progressDialog.Show(1);

                await _showDocumentStepProcessor.ProcessStepsAsync(progressDialog, steps);
            }
            catch (OperationCanceledException)
            {
                // Swallow if the user cancelled. We've broken out of the steps above.
                _publishContext.ToolkitShellProvider.OutputToHostConsole(
                    $"Launching the Publish to AWS dialog has been cancelled",
                    true);
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
    }
}
