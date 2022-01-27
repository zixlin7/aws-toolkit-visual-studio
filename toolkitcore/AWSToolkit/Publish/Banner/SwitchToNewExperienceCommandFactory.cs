using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.PluginServices.Publishing;

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
            await OpenNewExperience(publishBanner, publishToAws);
            publishBanner.CloseCurrentPublishExperience = true;
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
    }
}
