using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services
{
    public interface IConfigurationDetails
    {
        bool IsAddNewProfile { get; }

        ICredentialIdentifier SelectedCredentialIdentifier { get; }

        ProfileProperties ProfileProperties { get; set; }
    }
}
