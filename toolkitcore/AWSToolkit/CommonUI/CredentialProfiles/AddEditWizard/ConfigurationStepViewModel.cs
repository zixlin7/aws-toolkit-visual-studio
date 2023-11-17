using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    public class ConfigurationStepViewModel : StepViewModel
    {
        private CredentialType _selectedCredentialType;

        public CredentialType SelectedCredentialType
        {
            get => _selectedCredentialType;
            set => SetProperty(ref _selectedCredentialType, value);
        }

        private bool _credentialTypeSelectorVisible;

        public bool CredentialTypeSelectorVisible
        {
            get => _credentialTypeSelectorVisible;
            set => SetProperty(ref _credentialTypeSelectorVisible, value);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            SelectedCredentialType = CredentialType.SsoProfile;

            InitializeByFeatureType();
        }

        private void InitializeByFeatureType()
        {
            switch (_addEditProfileWizard.FeatureType)
            {
                case FeatureType.AwsExplorer:
                    CredentialTypeSelectorVisible = true;
                    break;
                case FeatureType.CodeWhisperer:
                    SelectedCredentialType = CredentialType.SsoProfile;
                    CredentialTypeSelectorVisible = false;
                    break;
            }
        }
    }
}
