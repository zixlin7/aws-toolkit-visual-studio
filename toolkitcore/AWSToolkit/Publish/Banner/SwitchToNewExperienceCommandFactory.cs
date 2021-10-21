using System;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.Publish.Banner
{
    public class SwitchToNewExperienceCommandFactory
    {
        private SwitchToNewExperienceCommandFactory() {}

        public static IAsyncCommand Create(PublishBannerViewModel publishBanner, IPublishToAws publishToAws)
        {
            return new AsyncRelayCommand((_) => SwitchToNewExperience(publishBanner, publishToAws));
        }

        private static async Task SwitchToNewExperience(PublishBannerViewModel publishBanner, IPublishToAws publishToAws)
        {
            Result result = Result.Failed;
            try
            {
                await HideOldPublishExperience(publishBanner.SettingsRepository);
                await OpenNewExperience(publishBanner, publishToAws);
                publishBanner.CloseCurrentPublishExperience = true;
                result = Result.Succeeded;
            }
            finally
            {
                publishBanner.RecordPublishOptInMetric(result);
            }
        }

        private static async Task OpenNewExperience(PublishBannerViewModel publishBanner, IPublishToAws publishToAws) 
        {
            var selectedProject = publishBanner.SelectedProject;
            var connectionManager = publishBanner.ToolkitContext.ConnectionManager;

            var args = new ShowPublishToAwsDocumentArgs()
            {
                ProjectName = selectedProject.Name,
                ProjectPath = selectedProject.Path,
                CredentialId = connectionManager.ActiveCredentialIdentifier,
                Region = connectionManager.ActiveRegion
            };

            await publishToAws.ShowPublishToAwsDocument(args);
        }

        private static async Task HideOldPublishExperience(IPublishSettingsRepository settingsRepository)
        {
            var settings = await settingsRepository.GetAsync();
            settings.ShowOldPublishExperience = false;
            settings.ShowPublishMenu = true;
            settingsRepository.Save(settings);
        }
    }
}
