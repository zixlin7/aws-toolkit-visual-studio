using System.Windows.Input;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Solutions;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.Publish.Banner
{
    public class PublishBannerViewModel : BaseModel
    {
        public ToolkitContext ToolkitContext { get; }

        public IPublishSettingsRepository SettingsRepository { get; }

        public Project SelectedProject { get; }

        public ICommand LearnMoreCommand { get; set; }

        public ICommand SwitchToNewExperienceCommand { get; set; }

        public bool ShowBanner => CanUseNewExperience();

        private bool CanUseNewExperience() => HostIs2019OrHigher() && IsProjectPublishable();

        private bool HostIs2019OrHigher() => ToolkitContext.ToolkitHostInfo != ToolkitHosts.Vs2017;

        private bool _closeCurrentPublishExperience = false;

        public string Origin { get; set; }

        public bool CloseCurrentPublishExperience
        {
            get => _closeCurrentPublishExperience;
            set => SetProperty(ref _closeCurrentPublishExperience, value);
        }

        public PublishBannerViewModel(ToolkitContext toolkitContext, IPublishSettingsRepository settingsRepository)
        {
            ToolkitContext = toolkitContext;
            SettingsRepository = settingsRepository;
            SelectedProject = toolkitContext.ToolkitHost.GetSelectedProject();
        }

        public void RecordPublishOptInMetric(Result result)
        {
            this.ToolkitContext.TelemetryLogger.RecordPublishOptIn(new PublishOptIn()
            {
                AwsAccount = ToolkitContext.ConnectionManager.ActiveAccountId,
                AwsRegion = ToolkitContext.ConnectionManager.ActiveRegion?.Id,
                ServiceType = Origin,
                Result = result
            });
        }

        private bool IsProjectPublishable()
        {
            return PublishableProjectSpecification.IsSatisfiedBy(SelectedProject?.TargetFramework);
        }
    }
}
