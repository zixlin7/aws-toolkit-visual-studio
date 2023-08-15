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

        internal GettingStartedViewModel(ToolkitContext toolkitContext)
            : base(toolkitContext) { }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            ToolkitSettings.Instance.HasUserSeenFirstRunForm = true;

            OpenAwsExplorerAsyncCommand = new OpenAwsExplorerCommand(_toolkitContext);
            OpenUsingToolkitDocsCommand = OpenUserGuideCommand.Create(_toolkitContext);

            Func<string, ICommand> openUrl = url => OpenUrlCommandFactory.Create(_toolkitContext, url);
            OpenDeployLambdaDocsCommand = openUrl("https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/lambda-cli-publish.html");
            OpenDeployBeanstalkDocsCommand = openUrl("https://docs.aws.amazon.com/elasticbeanstalk/latest/dg/create_deploy_NET.html");
            OpenDevBlogCommand = openUrl("https://aws.amazon.com/blogs/developer/category/net/");
            OpenPrivacyPolicyCommand = openUrl("https://aws.amazon.com/privacy/");

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

            return credIds.FirstOrDefault(ci => ci.ProfileName == "default") ?? credIds.FirstOrDefault();
        }
    }
}
