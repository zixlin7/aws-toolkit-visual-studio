using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Publish.Views.Dialogs;
using Amazon.AWSToolkit.Shared;

using log4net;

using Microsoft.VisualStudio.Threading;

namespace Amazon.AWSToolkit.Publish.Commands
{
    /// <summary>
    /// WPF command binding for Publish Document Panel's Publish Button
    /// </summary>
    public class PublishCommand : TargetViewCommand
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(PublishCommand));

        public delegate IConfirmPublishDialog ConfirmationDialogProducer(PublishToAwsDocumentViewModel viewModel);

        private readonly IAWSToolkitShellProvider _shellProvider;
        private readonly ConfirmationDialogProducer _confirmationFactory;

        public PublishCommand(PublishToAwsDocumentViewModel viewModel, IAWSToolkitShellProvider shellProvider)
            : this(viewModel, shellProvider, CreateConfirmationDialog)
        {
        }

        /// <summary>
        /// Overload to allow tests to provide a stub confirmation dialog
        /// </summary>
        public PublishCommand(PublishToAwsDocumentViewModel viewModel, IAWSToolkitShellProvider shellProvider,
            ConfirmationDialogProducer confirmationFactory)
            : base(viewModel)
        {
            _shellProvider = shellProvider;
            _confirmationFactory = confirmationFactory;
        }

        private static IConfirmPublishDialog CreateConfirmationDialog(PublishToAwsDocumentViewModel viewModel)
        {
            return new ConfirmPublishDialog()
            {
                ProjectName = viewModel.ProjectName,
                PublishDestinationName = viewModel.PublishDestination.Name,
                RegionName = viewModel.Connection.RegionDisplayName,
                CredentialsId = viewModel.Connection.CredentialsIdDisplayName,
            };
        }

        protected override async Task ExecuteCommandAsync()
        {
            try
            {
                if (!await PromptToPublishAsync())
                {
                    return;
                }

                // This case should not happen if the DeployTool is working correctly, but it was identified in IDE-7656 that it can occur if the DeployTool is altered
                // in an unexpected way.  This is just a safety net to give the user a clear indication what is wrong in case they missed the message in the output
                // window.
                if (!await PublishDocumentViewModel.ValidateTargetConfigurationsAsync())
                {
                    ShowValidationErrors();
                }

                await PublishDocumentViewModel.JoinableTaskFactory.SwitchToMainThreadAsync();

                PublishDocumentViewModel.PublishProjectViewModel.Clear();
                await PublishDocumentViewModel.UpdatePublishProjectViewModelAsync().ConfigureAwait(true);

                PublishDocumentViewModel.ViewStage = PublishViewStage.Publish;

                await TaskScheduler.Default;
                var publishResult = await PublishDocumentViewModel.PublishApplicationAsync().ConfigureAwait(false);

                await TaskScheduler.Default;
                if (publishResult.IsSuccess)
                {
                    await PublishDocumentViewModel.PublishProjectViewModel
                        .UpdatePublishedResourcesAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _shellProvider.OutputError(new Exception($"Publish to AWS failed: {e.Message}", e), Logger);
            }
        }

        private void ShowValidationErrors()
        {
            var sb = new StringBuilder();
            sb.AppendLine("One or more publish settings are not valid. Adjust the configuration and try again.");
            sb.AppendLine();

            foreach (var detail in PublishDocumentViewModel.ConfigurationDetails.GetDetailAndDescendants()
                         .Where(x => x.HasErrors))
            {
                sb.AppendLine(detail.Name);
            }

            var errmsg = sb.ToString();
            _shellProvider.ShowMessage("Publish to AWS failed", errmsg);
            throw new Exception(errmsg);
        }

        private async Task<bool> PromptToPublishAsync()
        {
            if (await IsPromptSuppressedAsync()) return true;

            var dlg = _confirmationFactory(PublishDocumentViewModel);

            var result = dlg.ShowModal() ?? false;

            if (result && dlg.SilenceFutureConfirmations)
            {
                await SuppressFuturePromptsAsync();
            }

            return result;
        }

        private async Task<bool> IsPromptSuppressedAsync()
        {
            var publishSettings = await PublishDocumentViewModel.PublishContext.PublishSettingsRepository.GetAsync();

            return publishSettings.SilencedPublishConfirmations.Any(silencedGuid =>
                silencedGuid.Equals(PublishDocumentViewModel.ProjectGuid.ToString(), StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task SuppressFuturePromptsAsync()
        {
            var publishSettings = await PublishDocumentViewModel.PublishContext.PublishSettingsRepository.GetAsync();
            publishSettings.SilencedPublishConfirmations.Add(PublishDocumentViewModel.ProjectGuid.ToString());

            PublishDocumentViewModel.PublishContext.PublishSettingsRepository.Save(publishSettings);
        }

        protected override bool CanExecuteCommand()
        {
            return !PublishDocumentViewModel.HasValidationErrors()
                   && !PublishDocumentViewModel.PublishProjectViewModel.IsPublishing
                   && !PublishDocumentViewModel.IsLoading
                   && !PublishDocumentViewModel.LoadingSystemCapabilities;
        }
    }
}
