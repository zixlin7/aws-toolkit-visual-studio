using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AWSToolkit.Urls;
using Amazon.AWSToolkit.VisualStudio.GettingStarted.Services;
using AwsToolkit.VsSdk.Common.Settings.CodeWhisperer;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
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

    internal class GettingStartedViewModel : RootViewModel, IAddEditProfileWizardHost, IGettingStarted
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GettingStartedViewModel));

        private IGettingStartedCompleted _gettingStartedCompleted => ServiceProvider.RequireService<IGettingStartedCompleted>();

        private GettingStartedStep _currentStep = GettingStartedStep.AddEditProfileWizards;

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

        private ICommand _openUsingToolkitDocsCommand;

        public ICommand OpenUsingToolkitDocsCommand
        {
            get => _openUsingToolkitDocsCommand;
            private set => SetProperty(ref _openUsingToolkitDocsCommand, value);
        }

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
            ServiceProvider.SetService<IGettingStarted>(this);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            SaveMetricSource = ToolkitSettings.Instance.HasUserSeenFirstRunForm ?
                MetricSources.GettingStartedMetricSource.GettingStarted :
                MetricSources.GettingStartedMetricSource.FirstStartup;

            ToolkitSettings.Instance.HasUserSeenFirstRunForm = true;

            ICommand OpenUrl(string url) => OpenUrlCommandFactory.Create(ToolkitContext, url);
            OpenAwsExplorerLearnMoreCommand = OpenUrl(AwsUrls.UserGuideWorkWithAws);
            OpenCodeWhispererLearnMoreCommand = OpenUrl(AwsUrls.CodeWhispererOverview);
            OpenGitHubCommand = OpenUrl(GitHubUrls.RepositoryUrl);

            OpenUsingToolkitDocsCommand = OpenUserGuideCommand.Create(ToolkitContext);

            FeatureType = IsCodeWhispererSupported
                ? FeatureType.CodeWhisperer
                : FeatureType.AwsExplorer;

            ShowInitialCard();
        }

        private void ShowInitialCard()
        {
            var codeWhispererCredId  = GetCodeWhispererCredentialIdentifierId();

            var credentialIdentifier = ToolkitContext.CredentialManager.GetCredentialIdentifierById(codeWhispererCredId);

            if (credentialIdentifier == null
                || FeatureType != FeatureType.CodeWhisperer
                || !credentialIdentifier.HasValidToken(ToolkitContext.CredentialSettingsManager.GetProfileProperties(credentialIdentifier)?.SsoSession, ToolkitContext.ToolkitHost))
            {
                return;
            }

            _gettingStartedCompleted.Status = true;
            ShowGettingStartedCompleted(credentialIdentifier);
        }

        public void ShowCompleted(ICredentialIdentifier credentialIdentifier)
        {
            if (FeatureType == FeatureType.CodeWhisperer)
            {
                SetCodeWhispererCredentialIdentifier(credentialIdentifier);
            }

            _gettingStartedCompleted.Status = true;
            ShowGettingStartedCompleted(credentialIdentifier);
        }

        private void SetCodeWhispererCredentialIdentifier(ICredentialIdentifier credentialIdentifier)
        {
            try
            {
                var settings = CodeWhispererSettings.Instance;
                settings.CredentialIdentifier = credentialIdentifier.Id;
                settings.Save();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to save CodeWhisperer credential identifier", ex);
            }
        }

        private string GetCodeWhispererCredentialIdentifierId()
        {
            try
            {
                return CodeWhispererSettings.Instance.CredentialIdentifier;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void ShowGettingStartedCompleted(ICredentialIdentifier credentialIdentifier)
        {
            _gettingStartedCompleted.CredentialFactoryId = credentialIdentifier.FactoryId;

            _gettingStartedCompleted.CredentialName = credentialIdentifier.ProfileName;

            CurrentStep = GettingStartedStep.GettingStartedCompleted;
        }
    }
}
