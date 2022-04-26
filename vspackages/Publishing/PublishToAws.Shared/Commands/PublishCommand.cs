using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

                if (!await PublishDocumentViewModel.ValidateTargetConfigurationsAsync())
                {
                    throw new Exception("One or more configuration settings are invalid.");
                }

                await PublishDocumentViewModel.ClearPublishedResourcesAsync(CancellationToken.None).ConfigureAwait(false);

                await PublishDocumentViewModel.JoinableTaskFactory.SwitchToMainThreadAsync();

                PublishDocumentViewModel.ViewStage = PublishViewStage.Publish;

                await TaskScheduler.Default;
                await PublishDocumentViewModel.PublishApplicationAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _shellProvider.OutputError(new Exception($"Publish to AWS failed: {e.Message}", e), Logger);
            }
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
                   && !PublishDocumentViewModel.IsPublishing
                   && !PublishDocumentViewModel.IsLoading
                   && !PublishDocumentViewModel.LoadingSystemCapabilities;
        }
    }
}
