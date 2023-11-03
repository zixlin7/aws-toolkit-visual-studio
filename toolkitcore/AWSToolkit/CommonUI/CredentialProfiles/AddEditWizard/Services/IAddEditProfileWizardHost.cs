using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services
{
    public interface IAddEditProfileWizardHost
    {
        BaseMetricSource SaveMetricSource { get; }

        void NotifyConnectionSettingsChanged(ICredentialIdentifier credentialIdentifier);
    }
}
