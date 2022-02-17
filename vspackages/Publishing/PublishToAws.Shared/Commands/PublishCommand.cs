using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;
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
        private readonly IAWSToolkitShellProvider _shellProvider;

        public PublishCommand(PublishToAwsDocumentViewModel viewModel, IAWSToolkitShellProvider shellProvider) :
            base(viewModel)
        {
            _shellProvider = shellProvider;
        }

        protected override async Task ExecuteCommandAsync()
        {
            try
            {
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

        protected override bool CanExecuteCommand()
        {
            return !PublishDocumentViewModel.HasValidationErrors() && !PublishDocumentViewModel.IsPublishing;
        }
    }
}
