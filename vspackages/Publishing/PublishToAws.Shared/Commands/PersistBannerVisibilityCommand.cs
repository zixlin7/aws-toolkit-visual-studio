using System;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tasks;

namespace Amazon.AWSToolkit.Publish.Commands
{
    /// <summary>
    /// Persists new publish experience options banner visibility to false in publish settings file
    /// </summary>
    public class PersistBannerVisibilityCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private readonly IPublishSettingsRepository _settingsRepository;
        private readonly PublishToAwsDocumentViewModel _publishDocumentViewModel;

        public PersistBannerVisibilityCommand(IPublishSettingsRepository settingsRepository,
            PublishToAwsDocumentViewModel publishToAwsDocumentViewModel)
        {
            _settingsRepository = settingsRepository;
            _publishDocumentViewModel = publishToAwsDocumentViewModel;
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            ExecuteAsync().LogExceptionAndForget();
        }

        private async Task ExecuteAsync()
        {
            await _publishDocumentViewModel.JoinableTaskFactory.SwitchToMainThreadAsync();
            _publishDocumentViewModel.IsOptionsBannerEnabled = false;

            var settings = await _settingsRepository.GetAsync();
            settings.ShowPublishBanner = false;
            _settingsRepository.Save(settings);
        }
    }
}
