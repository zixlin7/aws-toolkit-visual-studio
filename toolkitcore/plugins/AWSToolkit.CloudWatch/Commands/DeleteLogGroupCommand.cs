using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Shared;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Commands
{
    public class DeleteLogGroupCommand
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DeleteLogGroupCommand));

        public static IAsyncCommand Create(LogGroupsViewModel viewModel, IAWSToolkitShellProvider toolkitHost)
        {
            return new AsyncRelayCommand(parameter => Delete(viewModel, toolkitHost, parameter));
        }

        private static async Task Delete(LogGroupsViewModel viewModel, IAWSToolkitShellProvider toolkitHost, object parameter)
        {
            try
            {
                if (!(parameter is LogGroup logGroup))
                {
                    throw new ArgumentException($"Parameter is not of expected type: {typeof(LogGroup)}");
                }

                var message =
                    $"Are you sure you want to delete the following log group?{Environment.NewLine}{logGroup.Name}";
                if (!toolkitHost.Confirm("Delete Log Group", message))
                {
                    return;
                }

                var result = await viewModel.DeleteAsync(logGroup).ConfigureAwait(false);
                if (result)
                {
                    Refresh(viewModel);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error deleting log group", e);
                toolkitHost.ShowError("Error deleting log group", "Error deleting log group: " + e.Message);
            }
        }

        private static void Refresh(LogGroupsViewModel viewModel)
        {
            if (viewModel.RefreshCommand.CanExecute(null))
            {
                viewModel.RefreshCommand.Execute(null);
            }
        }
    }
}
