using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Shared;

using log4net;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace Amazon.AWSToolkit.CloudWatch.Commands
{
    public class DeleteLogGroupCommand
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(DeleteLogGroupCommand));

        public static IAsyncCommand Create(LogGroupsViewModel viewModel, IAWSToolkitShellProvider toolkitHost)
        {
            return new AsyncRelayCommand(parameter => ExecuteAsync(parameter, viewModel, toolkitHost));
        }

        private static async Task ExecuteAsync(object parameter, LogGroupsViewModel viewModel, IAWSToolkitShellProvider toolkitHost)
        {
            var result = await DeleteAsync(parameter, viewModel, toolkitHost);
            viewModel.RecordDeleteMetric(result);
        }

        private static async Task<TaskStatus> DeleteAsync(object parameter, LogGroupsViewModel viewModel,
            IAWSToolkitShellProvider toolkitHost)
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
                    return TaskStatus.Cancel;
                }

                var result = await viewModel.DeleteAsync(logGroup).ConfigureAwait(false);
                if (result)
                {
                    Refresh(viewModel);
                }

                return TaskStatus.Success;
            }
            catch (Exception e)
            {
                Logger.Error("Error deleting log group", e);
                toolkitHost.ShowError("Error deleting log group", "Error deleting log group: " + e.Message);

                return TaskStatus.Fail;
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
