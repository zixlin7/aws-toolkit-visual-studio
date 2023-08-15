using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Context;

namespace AwsToolkit.VsSdk.Common.CommonUI.Models
{
    public class CredentialProfileDialogViewModel : RootViewModel
    {
        private bool? _dialogResult;

        public bool? DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        public string Heading { get; } = "Setup a Profile to Authenticate";

        public CredentialProfileDialogViewModel(ToolkitContext toolkitContext)
            : base(toolkitContext) { }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var addEditProfileWizard = ServiceProvider.RequireService<IAddEditProfileWizard>();
            addEditProfileWizard.CredentialsFileOpened += (sender, e) => DialogResult = false;
            addEditProfileWizard.ConnectionSettingsChanged += (sender, e) => DialogResult = true;
        }
    }
}
