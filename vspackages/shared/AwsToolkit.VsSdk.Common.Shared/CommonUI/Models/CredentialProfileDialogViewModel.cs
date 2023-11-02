using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry.Model;

namespace AwsToolkit.VsSdk.Common.CommonUI.Models
{
    public class CredentialProfileDialogViewModel : RootViewModel, IAddEditProfileWizardHost
    {
        private bool? _dialogResult;

        public bool? DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        public string Heading { get; } = "Setup a Profile to Authenticate";

        public BaseMetricSource SaveMetricSource { get; private set; }

        public CredentialProfileDialogViewModel(ToolkitContext toolkitContext, BaseMetricSource saveMetricSource)
            : base(toolkitContext)
        {
            SaveMetricSource = saveMetricSource;
        }

        public override async Task RegisterServicesAsync()
        {
            await base.RegisterServicesAsync();

            ServiceProvider.SetService<IAddEditProfileWizardHost>(this);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var wizard = ServiceProvider.RequireService<IAddEditProfileWizard>();
            wizard.CredentialsFileOpened += (sender, e) => DialogResult = false;
        }

        public void NotifyConnectionSettingsChanged(ICredentialIdentifier credentialIdentifier, ToolkitRegion region)
        {
            DialogResult = true;
        }
    }
}
