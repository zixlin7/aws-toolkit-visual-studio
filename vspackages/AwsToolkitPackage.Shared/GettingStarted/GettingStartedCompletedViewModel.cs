using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Urls;
using Amazon.AWSToolkit.VisualStudio.GettingStarted.Services;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
    internal class GettingStartedCompletedViewModel : ViewModel, IGettingStartedCompleted
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GettingStartedCompletedViewModel));

        private IGettingStarted _gettingStarted =>
            ServiceProvider.RequireService<IGettingStarted>();

        public bool IsCodeWhispererSupported =>
#if VS2022_OR_LATER
                true;
#else
            false;
#endif

        private bool? _status;

        public bool? Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _featureTypeName;

        public string FeatureTypeName
        {
            get => _featureTypeName;
            set => SetProperty(ref _featureTypeName, value);
        }

        private string _credentialDisplayName;

        public string CredentialDisplayName
        {
            get => _credentialDisplayName;
            set => SetProperty(ref _credentialDisplayName, value);
        }

        private string _credentialFactoryId;

        public string CredentialFactoryId
        {
            get => _credentialFactoryId;
            set => SetProperty(ref _credentialFactoryId, value);
        }

        private string _credentialName;

        public string CredentialName
        {
            get => _credentialName;
            set => SetProperty(ref _credentialName, value);
        }

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

        private ICommand _openAddEditWizardCommand;

        public ICommand OpenAddEditWizardCommand
        {
            get => _openAddEditWizardCommand;
            private set => SetProperty(ref _openAddEditWizardCommand, value);
        }

        private ICommand _openDeployBeanstalkDocsCommand;

        public ICommand OpenDeployBeanstalkDocsCommand
        {
            get => _openDeployBeanstalkDocsCommand;
            private set => SetProperty(ref _openDeployBeanstalkDocsCommand, value);
        }

        private ICommand _openDeployLambdaDocsCommand;

        public ICommand OpenDeployLambdaDocsCommand
        {
            get => _openDeployLambdaDocsCommand;
            private set => SetProperty(ref _openDeployLambdaDocsCommand, value);
        }

        private ICommand _openDevBlogCommand;

        public ICommand OpenDevBlogCommand
        {
            get => _openDevBlogCommand;
            private set => SetProperty(ref _openDevBlogCommand, value);
        }

        private ICommand _openLogsCommand;

        public ICommand OpenLogsCommand
        {
            get => _openLogsCommand;
            private set => SetProperty(ref _openLogsCommand, value);
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

        private ICommand _openUsingToolkitDocsCommand;

        public ICommand OpenUsingToolkitDocsCommand
        {
            get => _openUsingToolkitDocsCommand;
            private set => SetProperty(ref _openUsingToolkitDocsCommand, value);
        }

        // TODO: IDE-11680 :  CodeWhisperer Tutorial Course
        //private ICommand _tryCodeWhispererExamplesAsyncCommand;

        //public ICommand TryCodeWhispererExamplesAsyncCommand
        //{
        //    get => _tryCodeWhispererExamplesAsyncCommand;
        //    private set => SetProperty(ref _tryCodeWhispererExamplesAsyncCommand, value);
        //}

        private ICommand _openCodeWhispererOverviewCommand;

        public ICommand OpenCodeWhispererOverviewCommand
        {
            get => _openCodeWhispererOverviewCommand;
            private set => SetProperty(ref _openCodeWhispererOverviewCommand, value);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            OpenAwsExplorerAsyncCommand = new OpenAwsExplorerCommand(ToolkitContext);
            OpenAddEditWizardCommand = new RelayCommand(OpenAddEditWizard);
            OpenLogsCommand = new OpenToolkitLogsCommand(ToolkitContext);
            OpenUsingToolkitDocsCommand = OpenUserGuideCommand.Create(ToolkitContext);

            // TODO: IDE-11680 :  CodeWhisperer Tutorial Course
            // TryCodeWhispererExamplesAsyncCommand = new RelayCommand(TryCodeWhispererExamples);

            ICommand OpenUrl(string url) => OpenUrlCommandFactory.Create(ToolkitContext, url);
            OpenPrivacyPolicyCommand = OpenUrl(AwsUrls.PrivacyPolicy);
            OpenTelemetryDisclosureCommand = OpenUrl(AwsUrls.TelemetryDisclosure);
            OpenDeployLambdaDocsCommand = OpenUrl(AwsUrls.DeployLambdaDocs);
            OpenDeployBeanstalkDocsCommand = OpenUrl(AwsUrls.DeployBeanstalkDocs);
            OpenDevBlogCommand = OpenUrl(AwsUrls.DevBlog);
            OpenCodeWhispererOverviewCommand = OpenUrl(AwsUrls.CodeWhispererOverview);
        }

        public override async Task RegisterServicesAsync()
        {
            await base.RegisterServicesAsync();

            ServiceProvider.SetService<IGettingStartedCompleted>(this);
        }

        public override async Task ViewLoadedAsync()
        {
            await base.ViewLoadedAsync();
            FeatureTypeName = _gettingStarted.FeatureType.GetDescription();
            CredentialDisplayName = CreateCredentialDisplayName();
        }

        public override async Task ViewShownAsync()
        {
            await base.ViewShownAsync();
            FeatureTypeName = _gettingStarted.FeatureType.GetDescription();
            CredentialDisplayName = CreateCredentialDisplayName();
        }

        private string CreateCredentialDisplayName()
        {
            return _credentialFactoryId != null && _credentialFactoryId.Equals("AwsBuilderId")
                ? $"AWS Builder ID ({_credentialName})"
                : $"IAM Identity Center ({_credentialName})";
        }

        // TODO: IDE-12124 : Open Add Edit Wizard Button
        // This command is still enabled for the New 2 CWSPR "Setup" button,
        // but will need to be added as a standalone button as well
        private void OpenAddEditWizard(object parameter)
        {
            _gettingStarted.FeatureType = IsCodeWhispererSupported
                ? FeatureType.CodeWhisperer
                : FeatureType.AwsExplorer;

            _gettingStarted.CurrentStep = GettingStartedStep.AddEditProfileWizards;
        }

        // TODO: IDE-11680 :  CodeWhisperer Tutorial Course
        //private void TryCodeWhispererExamples(object parameter)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
