using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Controls;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    public class SsoConfigurationDetailsViewModel : ConfigurationDetailsViewModel, ISsoProfilePropertiesProvider
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SsoConfigurationDetailsViewModel));

        public static readonly string ProfileRegionServiceName = $"{nameof(SsoConfigurationDetailsViewModel)}_ProfileRegion";

        public static readonly string SsoRegionServiceName = $"{nameof(SsoConfigurationDetailsViewModel)}_SsoRegion";

        public override CredentialType CredentialType => CredentialType.SsoProfile;

        #region ProfileName
        public string ProfileName
        {
            get => ProfileProperties.Name;
            set
            {
                ProfileProperties.Name = value;
                ValidateProfileName();
                NotifyPropertyChanged(nameof(ProfileName));
            }
        }

        private void ValidateProfileName()
        {
            DataErrorInfo.ClearErrors(nameof(ProfileName));

            if (string.IsNullOrWhiteSpace(ProfileName))
            {
                DataErrorInfo.AddError("Must not be empty and should contain alphanumeric characters, - or _", nameof(ProfileName));
                return;
            }
            var pattern = new Regex("^([a-zA-Z0-9_-])*$");
            var match = pattern.Match(ProfileName);
            if (!match.Success)
            {
                DataErrorInfo.AddError("Must contain alphanumeric, - or _ characters", nameof(ProfileName));
                return;
            }

            var allProfiles = _toolkitContext.CredentialManager.GetCredentialIdentifiers()
                .Where(credId => credId.FactoryId.Equals(SharedCredentialProviderFactory.SharedProfileFactoryId));
            var result = allProfiles.Select(credId => credId.ProfileName).Any(x => x.Equals(ProfileName));

            if (result)
            {
                DataErrorInfo.AddError("Name is not unique", nameof(ProfileName));
            }
        }
        #endregion

        #region SsoStartUrl
        public string SsoStartUrl
        {
            get => ProfileProperties.SsoStartUrl;
            set {
                ProfileProperties.SsoStartUrl = value;
                ValidateSsoStartUrl();
                NotifyPropertyChanged(nameof(SsoStartUrl));
            }
        }

        private void ValidateSsoStartUrl()
        {
            var p = ProfileProperties;

            DataErrorInfo.ClearErrors(nameof(p.SsoStartUrl));

            if (string.IsNullOrWhiteSpace(p.SsoStartUrl))
            {
                DataErrorInfo.AddError("Must not be empty", nameof(p.SsoStartUrl));
                return;
            }

            var pattern = new Regex("^http(s)?://.*\\.awsapps\\.com/start$");
            var match = pattern.Match(p.SsoStartUrl);
            if (!match.Success)
            {
                DataErrorInfo.AddError("Url must be of format - `https://<YOUR_SUBDOMAIN>.awsapps.com/start`", nameof(p.SsoStartUrl));
            }
        }
        #endregion

        private RegionSelectorMixin _ssoRegionSelectorMixin;

        public RegionSelectorMixin SsoRegionSelectorMixin
        {
            get => _ssoRegionSelectorMixin;
            private set => SetProperty(ref _ssoRegionSelectorMixin, value);
        }

        #region ConnectToIamIdentityCenter

        private ICommand _connectToIamIdentityCenterCommand;

        public ICommand ConnectToIamIdentityCenterCommand
        {
            get => _connectToIamIdentityCenterCommand;
            private set => SetProperty(ref _connectToIamIdentityCenterCommand, value);
        }

        private void ConnectToIamIdentityCenter(object parameter)
        {
            _addEditProfileWizard.CurrentStep = WizardStep.SsoConnecting;
        }

        private bool CanConnectToIamIdentityCenter(object parameter)
        {
            if (string.IsNullOrWhiteSpace(ProfileName))
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(SsoStartUrl) && !DataErrorInfo.HasErrors;
        }
        #endregion

        public override async Task RegisterServicesAsync()
        {
            await base.RegisterServicesAsync();

            ServiceProvider.SetService<ISsoProfilePropertiesProvider>(this);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            ProfileRegionSelectorMixin = new RegionSelectorMixin(_toolkitContext, region => ProfileProperties.Region = region.Id);
            SsoRegionSelectorMixin = new RegionSelectorMixin(_toolkitContext, region => ProfileProperties.SsoRegion = region.Id);

            ConnectToIamIdentityCenterCommand = new RelayCommand(CanConnectToIamIdentityCenter, ConnectToIamIdentityCenter);

            SelectedCredentialFileType = CredentialFileType.Shared;
        }
    }
}
