using System;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tasks;

namespace Amazon.AWSToolkit.Publish.Commands
{
    public class CloseFailureBannerCommandFactory
    {
        private CloseFailureBannerCommandFactory() {}

        public static IAsyncCommand Create(PublishToAwsDocumentViewModel publishDocumentViewModel)
        {
            return new AsyncRelayCommand((_) => CloseFailureBannerAsync(publishDocumentViewModel));
        }

        private static async Task CloseFailureBannerAsync(PublishToAwsDocumentViewModel publishDocumentViewModel)
        {
            await publishDocumentViewModel.JoinableTaskFactory.SwitchToMainThreadAsync();
            publishDocumentViewModel.IsFailureBannerEnabled = false;
        }
    }
}
