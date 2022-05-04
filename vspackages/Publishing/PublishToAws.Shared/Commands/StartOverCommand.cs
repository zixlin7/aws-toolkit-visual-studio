using System;
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
                    PublishDocumentViewModel.ErrorMessage = string.Empty;
                    var initialIsRepublishValue = PublishDocumentViewModel.IsRepublish;

                    await PublishDocumentViewModel.RestartDeploymentSessionAsync(cancellationToken)
                        .ConfigureAwait(false);

                    await PublishDocumentViewModel.InitializePublishTargetsAsync(cancellationToken).ConfigureAwait(false);

                    // If user chose a new publish target and the publish failed, re-choose that setting
                    if (PublishDocumentViewModel.PublishProjectViewModel.ProgressStatus == ProgressStatus.Fail)
                    {
                        await PublishDocumentViewModel.SetIsRepublishAsync(initialIsRepublishValue, cancellationToken).ConfigureAwait(false);
                    }

                    await PublishDocumentViewModel.JoinableTaskFactory.SwitchToMainThreadAsync();
                    PublishDocumentViewModel.ViewStage = PublishViewStage.Target;
                    PublishDocumentViewModel.PublishProjectViewModel.Clear();
                }
                catch (Exception e)
                {
                    PublishDocumentViewModel.PublishContext.ToolkitShellProvider.OutputError(new Exception($"Failed to reset the Publish to AWS view: {e.Message}", e), Logger);

                    await PublishDocumentViewModel.JoinableTaskFactory.SwitchToMainThreadAsync();
                    PublishDocumentViewModel.ErrorMessage = e.Message;
                }
            }
        }
    }
}
