using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Publish.Views;

using log4net;

namespace Amazon.AWSToolkit.Publish.Commands
{
    /// <summary>
    /// Resets the publish experience state and brings users to the Target selection screen.
    /// Intended for use after completing a Publish.
    /// </summary>
    public class StartOverCommand : PublishFooterCommand
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(StartOverCommand));
        private readonly PublishToAwsDocumentControl _control;

        public StartOverCommand(PublishToAwsDocumentViewModel viewModel,
            PublishToAwsDocumentControl publishToAwsDocumentControl) : base(viewModel)
        {
            _control = publishToAwsDocumentControl;
        }

        public override bool CanExecute(object paramter)
        {
            return PublishDocumentViewModel.ViewStage == PublishViewStage.Publish
                && !PublishDocumentViewModel.PublishProjectViewModel.IsPublishing;
        }

        protected override async Task ExecuteCommandAsync()
        {
            var cancellationToken = PublishDocumentViewModel.PublishContext.PublishPackage.DisposalToken;
            using (PublishDocumentViewModel.CreateLoadingScope())
            {
                try
                {
                    var publishDestination = PublishDocumentViewModel.PublishDestination;

                    await PublishDocumentViewModel.ClearTargetSelectionAsync(cancellationToken).ConfigureAwait(false);

                    await PublishDocumentViewModel.RestartDeploymentSessionAsync(cancellationToken)
                        .ConfigureAwait(false);

                    await PublishDocumentViewModel.InitializePublishTargetsAsync(cancellationToken)
                        .ConfigureAwait(false);

                    await UpdateSelectedTargetAsync(publishDestination, cancellationToken).ConfigureAwait(false);

                    await PublishDocumentViewModel.JoinableTaskFactory.SwitchToMainThreadAsync();
                    PublishDocumentViewModel.ViewStage = PublishViewStage.Target;
                    PublishDocumentViewModel.PublishProjectViewModel.Clear();
                }
                catch (Exception e)
                {
                    PublishDocumentViewModel.PublishContext.ToolkitShellProvider.OutputError(
                        new Exception($"Failed to reset the Publish to AWS view: {e.Message}", e), Logger);

                    await PublishDocumentViewModel.JoinableTaskFactory.SwitchToMainThreadAsync();
                    PublishDocumentViewModel.ErrorMessage = e.Message;
                }
            }
        }

        private async Task UpdateSelectedTargetAsync(PublishDestinationBase publishDestination, CancellationToken cancellationToken)
        {
            if (publishDestination is PublishRecommendation recommendation)
            {
                if (PublishDocumentViewModel.PublishProjectViewModel.ProgressStatus == ProgressStatus.Success)
                {
                    // The new deployment was successful, so take users to the redeployment list, and select the corresponding entry
                    await PublishDocumentViewModel.SetTargetSelectionModeAsync(TargetSelectionMode.ExistingTargets, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Re-select the same recommendation, within the new deployment list
                    await PublishDocumentViewModel.JoinableTaskFactory.SwitchToMainThreadAsync();
                    PublishDocumentViewModel.PublishDestination =
                        PublishDocumentViewModel.GetRecommendationOrFallback(recommendation);
                }
            }
            else if (publishDestination is RepublishTarget republishTarget)
            {
                // Pass or fail, keep users in the redeployment list, and select the same entry
                await PublishDocumentViewModel.JoinableTaskFactory.SwitchToMainThreadAsync();
                PublishDocumentViewModel.PublishDestination =
                    PublishDocumentViewModel.GetRepublishTargetOrFallback(republishTarget);
            }
        }
    }
}
