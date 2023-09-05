using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services
{
    public interface ISsoProfilePropertiesProvider
    {
        ProfileProperties ProfileProperties { get; }
    }
}
