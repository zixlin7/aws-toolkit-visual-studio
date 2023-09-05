using System.Threading.Tasks;

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

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            SelectedCredentialType = CredentialType.SsoProfile;
        }
    }
}
