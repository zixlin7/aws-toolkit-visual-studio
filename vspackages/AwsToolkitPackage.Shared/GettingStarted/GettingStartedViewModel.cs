using System;
using System.Linq;
using System.Threading.Tasks;
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
using Amazon.AWSToolkit.Urls;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
    public enum GettingStartedStep
    {
        AddEditProfileWizard,
        GettingStarted
    }

    public class GettingStartedViewModel : RootViewModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GettingStartedViewModel));

        private IAddEditProfileWizard _addEditProfileWizard => ServiceProvider.RequireService<IAddEditProfileWizard>();

        #region GettingStartedStep

        private GettingStartedStep _currentStep;

        public GettingStartedStep CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }
        #endregion

        private string _credentialTypeName;

        public string CredentialTypeName
        {
            get => _credentialTypeName;
            set => SetProperty(ref _credentialTypeName, value);
        }

        private string _credentialName;

        public string CredentialName
        {
            get => _credentialName;
            set => SetProperty(ref _credentialName, value);
        }

        #region Status
        private bool? _status;

        public bool? Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        #endregion

        #region CollectAnalytics
        public bool CollectAnalytics
        {
            get
            {
                try
                {
                    return ToolkitSettings.Instance.TelemetryEnabled;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }

                return ToolkitSettings.DefaultValues.TelemetryEnabled;
            }
            set
            {
                try
                {
                    if (ToolkitSettings.Instance.TelemetryEnabled != value)
                    {
                        ToolkitSettings.Instance.TelemetryEnabled = value;
                        NotifyPropertyChanged(nameof(CollectAnalytics));
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }
        }
        #endregion

        private ICommand _openAwsExplorerAsyncCommand;

        public ICommand OpenAwsExplorerAsyncCommand
        {
            get => _openAwsExplorerAsyncCommand;
            private set => SetProperty(ref _openAwsExplorerAsyncCommand, value);
        }

        private ICommand _openGitHubCommand;

        public ICommand OpenGitHubCommand
        {
            get => _openGitHubCommand;
            private set => SetProperty(ref _openGitHubCommand, value);
        }

        private ICommand _openUsingToolkitDocsCommand;

        public ICommand OpenUsingToolkitDocsCommand
        {
            get => _openUsingToolkitDocsCommand;
            private set => SetProperty(ref _openUsingToolkitDocsCommand, value);
        }

        private ICommand _openDeployLambdaDocsCommand;

        public ICommand OpenDeployLambdaDocsCommand
        {
            get => _openDeployLambdaDocsCommand;
            private set => SetProperty(ref _openDeployLambdaDocsCommand, value);
        }

        private ICommand _openDeployBeanstalkDocsCommand;

        public ICommand OpenDeployBeanstalkDocsCommand
        {
            get => _openDeployBeanstalkDocsCommand;
            private set => SetProperty(ref _openDeployBeanstalkDocsCommand, value);
        }

        private ICommand _openDevBlogCommand;

        public ICommand OpenDevBlogCommand
        {
            get => _openDevBlogCommand;
            private set => SetProperty(ref _openDevBlogCommand, value);
        }

        private ICommand _openPrivacyPolicyCommand;

        public ICommand OpenPrivacyPolicyCommand
        {
            get => _openPrivacyPolicyCommand;
            private set => SetProperty(ref _openPrivacyPolicyCommand, value);
        }

        private ICommand _openTelemetryDisclosureCommand;

        public ICommand OpenTelemetryDisclosureCommand
        {
            get => _openTelemetryDisclosureCommand;
            private set => SetProperty(ref _openTelemetryDisclosureCommand, value);
        }

        private ICommand _openLogsCommand;

        public ICommand OpenLogsCommand
        {
            get => _openLogsCommand;
            private set => SetProperty(ref _openLogsCommand, value);
        }

        internal GettingStartedViewModel(ToolkitContext toolkitContext)
            : base(toolkitContext) { }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            ToolkitSettings.Instance.HasUserSeenFirstRunForm = true;

            _addEditProfileWizard.SaveMetricSource = MetricSources.GettingStartedMetricSource.GettingStarted;

            OpenAwsExplorerAsyncCommand = new OpenAwsExplorerCommand(_toolkitContext);
            OpenUsingToolkitDocsCommand = OpenUserGuideCommand.Create(_toolkitContext);
            OpenLogsCommand = new OpenToolkitLogsCommand(_toolkitContext);

            Func<string, ICommand> openUrl = url => OpenUrlCommandFactory.Create(_toolkitContext, url);
            OpenGitHubCommand = openUrl(GitHubUrls.RepositoryUrl);
            OpenDeployLambdaDocsCommand = openUrl(AwsUrls.DeployLambdaDocs);
            OpenDeployBeanstalkDocsCommand = openUrl(AwsUrls.DeployBeanstalkDocs);
            OpenDevBlogCommand = openUrl(AwsUrls.DevBlog);
            OpenPrivacyPolicyCommand = openUrl(AwsUrls.PrivacyPolicy);
            OpenTelemetryDisclosureCommand = openUrl(AwsUrls.TelemetryDisclosure);

            await ShowInitialCardAsync();
        }

        private async Task ShowInitialCardAsync()
        {
            var credId = GetDefaultCredentialIdentifier();
            if (credId == null)
            {
                _addEditProfileWizard.ConnectionSettingsChanged += (sender, e) =>
                {
                    Status = true;
                    ShowGettingStarted(e.CredentialIdentifier);
                };
                CurrentStep = GettingStartedStep.AddEditProfileWizard;
            }
            else
            {
                ShowGettingStarted(credId);
                await ChangeConnectionSettingsAsync(credId);
            }
        }

        private async Task ChangeConnectionSettingsAsync(ICredentialIdentifier credentialIdentifier)
        {
            var profileProperties = _toolkitContext.CredentialSettingsManager.GetProfileProperties(credentialIdentifier);
            var region = _toolkitContext.RegionProvider.GetRegion(profileProperties.Region ?? ToolkitRegion.DefaultRegionId);
            try
            {
                var state = await _toolkitContext.ConnectionManager.ChangeConnectionSettingsAsync(credentialIdentifier, region);
                Status = ConnectionState.IsValid(state);
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to set AWS Explorer to existing credential.", ex);
                Status = false;
            }
        }

        private void ShowGettingStarted(ICredentialIdentifier credentialIdentifier)
        {
            var profileProperties = _toolkitContext.CredentialSettingsManager.GetProfileProperties(credentialIdentifier);

            CredentialTypeName = profileProperties.GetCredentialType().GetDescription();
            CredentialName = credentialIdentifier.ProfileName;
            CurrentStep = GettingStartedStep.GettingStarted;
        }

        private ICredentialIdentifier GetDefaultCredentialIdentifier()
        {
            var credIds = _toolkitContext.CredentialManager.GetCredentialIdentifiers().Where(ci =>
                ci.FactoryId == SDKCredentialProviderFactory.SdkProfileFactoryId ||
                ci.FactoryId == SharedCredentialProviderFactory.SharedProfileFactoryId);

            return
                _toolkitContext.ConnectionManager.ActiveCredentialIdentifier ??
                credIds.FirstOrDefault(ci => ci.ProfileName == "default") ??
                credIds.FirstOrDefault();
        }
    }
}
