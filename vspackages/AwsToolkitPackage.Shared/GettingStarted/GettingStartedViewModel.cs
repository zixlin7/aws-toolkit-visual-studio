using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AWSToolkit.Urls;
using Amazon.AWSToolkit.VisualStudio.GettingStarted.Services;

using AwsToolkit.VsSdk.Common.Settings.CodeWhisperer;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
    public enum GettingStartedStep
    {
        AddEditProfileWizards,
        GettingStartedCompleted
    }

    public class RadioButtonEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : Binding.DoNothing;
        }
    }

    internal class GettingStartedViewModel : RootViewModel, IAddEditProfileWizardHost
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GettingStartedViewModel));

        private IGettingStartedCompleted _gettingStartedCompleted => ServiceProvider.RequireService<IGettingStartedCompleted>();

        private GettingStartedStep _currentStep;

        public GettingStartedStep CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }

        private FeatureType _featureType;

        public FeatureType FeatureType
        {
            get => _featureType;
            set => SetProperty(ref _featureType, value);
        }

        public bool IsCodeWhispererSupported =>
#if VS2022_OR_LATER
                true;
#else
                false;
#endif

        private ICommand _openAwsExplorerLearnMoreCommand;

        public ICommand OpenAwsExplorerLearnMoreCommand
        {
            get => _openAwsExplorerLearnMoreCommand;
            private set => SetProperty(ref _openAwsExplorerLearnMoreCommand, value);
        }

        private ICommand _openCodeWhispererLearnMoreCommand;

        public ICommand OpenCodeWhispererLearnMoreCommand
        {
            get => _openCodeWhispererLearnMoreCommand;
            private set => SetProperty(ref _openCodeWhispererLearnMoreCommand, value);
        }

        private ICommand _openGitHubCommand;

        public ICommand OpenGitHubCommand
        {
            get => _openGitHubCommand;
            private set => SetProperty(ref _openGitHubCommand, value);
        }

        public BaseMetricSource SaveMetricSource { get; private set; }

        internal GettingStartedViewModel(ToolkitContext toolkitContext)
            : base(toolkitContext) { }

        public override async Task RegisterServicesAsync()
        {
            await base.RegisterServicesAsync();

            ServiceProvider.SetService<IAddEditProfileWizardHost>(this);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            SaveMetricSource = ToolkitSettings.Instance.HasUserSeenFirstRunForm ?
                MetricSources.GettingStartedMetricSource.GettingStarted :
                MetricSources.GettingStartedMetricSource.FirstStartup;

            ToolkitSettings.Instance.HasUserSeenFirstRunForm = true;

            ICommand openUrl(string url) => OpenUrlCommandFactory.Create(ToolkitContext, url);
            OpenAwsExplorerLearnMoreCommand = openUrl(AwsUrls.UserGuideWorkWithAws);
            OpenCodeWhispererLearnMoreCommand = openUrl(AwsUrls.CodeWhispererOverview);
            OpenGitHubCommand = openUrl(GitHubUrls.RepositoryUrl);

            if (!IsCodeWhispererSupported)
            {
                FeatureType = FeatureType.AwsExplorer;
            }

            await ShowInitialCardAsync();
        }

        private async Task ShowInitialCardAsync()
        {
            var credId = GetDefaultCredentialIdentifier();
            if (credId == null)
            {
                CurrentStep = GettingStartedStep.AddEditProfileWizards;
            }
            else
            {
                ShowGettingStarted(credId);
                await ChangeConnectionSettingsAsync(credId);
            }
        }

        public void ShowCompleted(ICredentialIdentifier credentialIdentifier)
        {
            if (FeatureType == FeatureType.CodeWhisperer)
            {
                SetCodeWhispererCredentialIdentifier(credentialIdentifier);
            }

            _gettingStartedCompleted.Status = true;
            ShowGettingStarted(credentialIdentifier);
        }

        private void SetCodeWhispererCredentialIdentifier(ICredentialIdentifier credentialIdentifier)
        {
            var settings = CodeWhispererSettings.Instance;
            settings.CredentialIdentifier = credentialIdentifier.Id;
            settings.Save();
        }

        private async Task ChangeConnectionSettingsAsync(ICredentialIdentifier credentialIdentifier)
        {
            var profileProperties = ToolkitContext.CredentialSettingsManager.GetProfileProperties(credentialIdentifier);
            var region = ToolkitContext.RegionProvider.GetRegion(profileProperties.Region ?? ToolkitRegion.DefaultRegionId);
            try
            {
                var state = await ToolkitContext.ConnectionManager.ChangeConnectionSettingsAsync(credentialIdentifier, region);
                _gettingStartedCompleted.Status = ConnectionState.IsValid(state);
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to set AWS Explorer to existing credential.", ex);
                _gettingStartedCompleted.Status = false;
            }
        }

        private void ShowGettingStarted(ICredentialIdentifier credentialIdentifier)
        {
            var profileProperties = ToolkitContext.CredentialSettingsManager.GetProfileProperties(credentialIdentifier);

            _gettingStartedCompleted.CredentialTypeName = profileProperties.GetCredentialType().GetDescription();
            _gettingStartedCompleted.CredentialName = credentialIdentifier.ProfileName;
            CurrentStep = GettingStartedStep.GettingStartedCompleted;
        }

        private ICredentialIdentifier GetDefaultCredentialIdentifier()
        {
            var credIds = ToolkitContext.CredentialManager.GetCredentialIdentifiers().Where(ci =>
                ci.FactoryId == SDKCredentialProviderFactory.SdkProfileFactoryId ||
                ci.FactoryId == SharedCredentialProviderFactory.SharedProfileFactoryId);

            return
                ToolkitContext.ConnectionManager.ActiveCredentialIdentifier ??
                credIds.FirstOrDefault(ci => ci.ProfileName == "default") ??
                credIds.FirstOrDefault();
        }
    }
}
