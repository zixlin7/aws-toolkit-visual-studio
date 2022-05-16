using System.Threading.Tasks;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Publish.ViewModels;

using Microsoft.VisualStudio.Threading;

namespace Amazon.AWSToolkit.Publish.Commands
{
    public class CloseFailureBannerCommandFactory
    {
        private CloseFailureBannerCommandFactory() {}

        public static IAsyncCommand Create(PublishProjectViewModel viewModel, JoinableTaskFactory joinableTaskFactory)
        {
            return new AsyncRelayCommand((_) => CloseFailureBannerAsync(viewModel, joinableTaskFactory));
        }

        private static async Task CloseFailureBannerAsync(PublishProjectViewModel viewModel,
            JoinableTaskFactory joinableTaskFactory)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync();
            viewModel.IsFailureBannerEnabled = false;
        }
    }
}
