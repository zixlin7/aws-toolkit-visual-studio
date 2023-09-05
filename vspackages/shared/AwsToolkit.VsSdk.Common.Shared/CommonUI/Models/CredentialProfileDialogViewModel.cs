using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Telemetry.Model;

namespace AwsToolkit.VsSdk.Common.CommonUI.Models
{
    public class CredentialProfileDialogViewModel : RootViewModel
    {
        private readonly BaseMetricSource _saveMetricSource;

        private bool? _dialogResult;

        public bool? DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        public string Heading { get; } = "Setup a Profile to Authenticate";

        public CredentialProfileDialogViewModel(ToolkitContext toolkitContext, BaseMetricSource saveMetricSource)
            : base(toolkitContext)
        {
            _saveMetricSource = saveMetricSource;
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var wizard = ServiceProvider.RequireService<IAddEditProfileWizard>();
            wizard.SaveMetricSource = _saveMetricSource;
            wizard.CredentialsFileOpened += (sender, e) => DialogResult = false;
            wizard.ConnectionSettingsChanged += (sender, e) => DialogResult = true;
        }
    }
}
