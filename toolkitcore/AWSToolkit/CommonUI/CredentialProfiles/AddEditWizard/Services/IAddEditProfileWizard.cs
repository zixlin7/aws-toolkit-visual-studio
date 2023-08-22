using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services
{
    public enum WizardStep
    {
        Configuration,
        SsoConnecting,
        SsoConnected
    }

    public enum CredentialFileType
    {
        Sdk,
        Shared
    }

    public class CredentialsFileOpenedEventArgs : EventArgs
    {
        public static readonly CredentialsFileOpenedEventArgs Empty = new CredentialsFileOpenedEventArgs();
    }

    public interface IAddEditProfileWizard
    {
        WizardStep CurrentStep { get; set; }

        bool InProgress { get; set; }

        Task SaveAsync(ProfileProperties profileProperties, CredentialFileType fileType, bool changeConnectionSettings = true);

        event EventHandler<CredentialsFileOpenedEventArgs> CredentialsFileOpened;

        event EventHandler<ConnectionSettingsChangeArgs> ConnectionSettingsChanged;
    }
}
