using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Commands;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Commands
{
    /// <summary>
    /// Responsible for refreshing log resources
    /// </summary>
    public class RefreshLogsCommand
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(RefreshLogsCommand));

        public static IAsyncCommand Create(BaseLogsViewModel viewModel)
        {
            return new AsyncRelayCommand(_ => ExecuteAsync(viewModel));
        }

        private static async Task ExecuteAsync(BaseLogsViewModel viewModel)
        {
            await RefreshAsync(viewModel);
            viewModel.RecordRefreshMetric();
        }

        private static async Task RefreshAsync(BaseLogsViewModel viewModel)
        {
            try
            {
                viewModel.ResetCancellationToken();
                await viewModel.RefreshAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var logType = viewModel.GetLogTypeDisplayName();
                Logger.Error($"Error refreshing {logType}", e);
                viewModel.SetErrorMessage($"Error refreshing {logType}:{Environment.NewLine}{e.Message}");
            }
        }
    }
}
