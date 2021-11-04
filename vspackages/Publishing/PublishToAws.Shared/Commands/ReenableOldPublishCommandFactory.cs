using System;
using System.Threading.Tasks;
using System.Windows;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Publish.Views;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.Publish.Commands
{
    /// <summary>
    /// Creates a WPF command that re-enables the old publish experience
    /// </summary>
    public class ReenableOldPublishCommandFactory
    {
        public static IAsyncCommand Create(PublishToAwsDocumentViewModel viewModel)
        {
            return new AsyncRelayCommand((obj) => ReenableOldExperienceAsync(viewModel));
        }

        private static async Task ReenableOldExperienceAsync(PublishToAwsDocumentViewModel viewModel)
        {
            Result result = Result.Failed;
            try
            {
                await EnableOldExperienceAsync(viewModel.PublishContext.PublishSettingsRepository);
                viewModel.LoadPublishSettings();
                ShowMessageDialog(viewModel);
                result = Result.Succeeded;
            }
            finally
            {
                viewModel.RecordOptOutMetric(result);
            }

        }

        private static async Task EnableOldExperienceAsync(IPublishSettingsRepository settingsRepository)
        {
            var settings = await settingsRepository.GetAsync();
            settings.ShowOldPublishExperience = true;
            settingsRepository.Save(settings);
        }

        private static void ShowMessageDialog(PublishToAwsDocumentViewModel viewModel)
        {
            var dialog = new ReenableOldPublishDialog(viewModel);
            viewModel.PublishContext.ToolkitShellProvider.ShowInModalDialogWindow(dialog, MessageBoxButton.OK);
        }
    }
}
