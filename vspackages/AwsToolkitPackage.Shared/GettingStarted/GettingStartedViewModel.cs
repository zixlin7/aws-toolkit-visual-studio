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
using Amazon.AWSToolkit.VisualStudio.GettingStarted.Services;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
    public enum GettingStartedStep
    {
        AddEditProfileWizard,
        GettingStartedCompleted
    }

    public class GettingStartedViewModel : RootViewModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GettingStartedViewModel));

        private IAddEditProfileWizard _addEditProfileWizard => ServiceProvider.RequireService<IAddEditProfileWizard>();

        private IGettingStartedCompleted _gettingStartedCompleted => ServiceProvider.RequireService<IGettingStartedCompleted>();

        #region GettingStartedStep

        private GettingStartedStep _currentStep;

        public GettingStartedStep CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }
        #endregion

        private ICommand _openGitHubCommand;

        public ICommand OpenGitHubCommand
        {
            get => _openGitHubCommand;
            private set => SetProperty(ref _openGitHubCommand, value);
        }

        internal GettingStartedViewModel(ToolkitContext toolkitContext)
            : base(toolkitContext) { }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            _addEditProfileWizard.SaveMetricSource = ToolkitSettings.Instance.HasUserSeenFirstRunForm ?
                MetricSources.GettingStartedMetricSource.GettingStarted :
                MetricSources.GettingStartedMetricSource.FirstStartup;

            ToolkitSettings.Instance.HasUserSeenFirstRunForm = true;

            OpenGitHubCommand = OpenUrlCommandFactory.Create(ToolkitContext, GitHubUrls.RepositoryUrl);

            await ShowInitialCardAsync();
        }

        private async Task ShowInitialCardAsync()
        {
            var credId = GetDefaultCredentialIdentifier();
            if (credId == null)
            {
                _addEditProfileWizard.ConnectionSettingsChanged += (sender, e) =>
                {
                    _gettingStartedCompleted.Status = true;
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
