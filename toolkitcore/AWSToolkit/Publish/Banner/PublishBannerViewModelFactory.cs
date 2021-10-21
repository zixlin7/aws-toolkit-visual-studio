using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.Publish.Banner
{
    public class PublishBannerViewModelFactory
    {
        public static PublishBannerViewModel Create(ToolkitContext toolkitContext)
        {
            var publishBanner = CreateViewModel(toolkitContext);

            publishBanner.LearnMoreCommand = CreateLearnMoreCommand(toolkitContext.ToolkitHost);
            publishBanner.SwitchToNewExperienceCommand = CreateSwitchToNewExperienceCommand(publishBanner);

            return publishBanner;
        }

        private static PublishBannerViewModel CreateViewModel(ToolkitContext toolkitContext)
        {
            return new PublishBannerViewModel(toolkitContext, new FilePublishSettingsRepository());
        }

        private static ICommand CreateLearnMoreCommand(IAWSToolkitShellProvider toolkitHost)
        {
            return new ShowExceptionAndForgetCommand(LearnMoreCommandFactory.Create(toolkitHost), toolkitHost);
        }

        private static ICommand CreateSwitchToNewExperienceCommand(PublishBannerViewModel publishBanner)
        {
            var publishToAws = GetPublishToAws(publishBanner);
            return SwitchToNewExperienceCommandFactory.Create(publishBanner, publishToAws);
        }

        private static IPublishToAws GetPublishToAws(PublishBannerViewModel publishBanner)
        {
            return publishBanner.ToolkitContext.ToolkitHost.QueryShellProviderService<SPublishToAws>() as IPublishToAws;
        }
    }
}
