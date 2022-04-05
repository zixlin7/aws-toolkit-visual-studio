using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Commands;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Commands
{
    public class RefreshLogGroupsCommand
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(RefreshLogGroupsCommand));

        public static IAsyncCommand Create(LogGroupsViewModel viewModel)
        {
            return new AsyncRelayCommand(_ => RefreshAsync(viewModel));
        }

        private static async Task RefreshAsync(LogGroupsViewModel viewModel)
        {
            try
            {
                viewModel.ResetCancellationToken();
                await viewModel.RefreshAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error("Error refreshing log groups", e);
                viewModel.SetErrorMessage($"Error refreshing log groups:{Environment.NewLine}{e.Message}");
            }
        }
    }
}
