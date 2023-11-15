using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services
{
    public enum FeatureType
    {
        NotSet,
        AwsExplorer,
        CodeWhisperer
    }

    public enum WizardStep
    {
        Configuration,
        SsoConnecting,
        SsoAwsCredentialConnected,
        SsoBearerTokenConnected
    }

    public enum CredentialFileType
    {
        Sdk,
        Shared
    }

    public class CredentialsFileOpenedEventArgs : EventArgs
    {
        public static new readonly CredentialsFileOpenedEventArgs Empty = new CredentialsFileOpenedEventArgs();
    }

    public class SaveAsyncResults
    {
        public ActionResults ActionResults { get; }

        public ICredentialIdentifier CredentialIdentifier { get; }

        public SaveAsyncResults(ActionResults actionResults, ICredentialIdentifier credentialIdentifier)
        {
            ActionResults = actionResults;
            CredentialIdentifier = credentialIdentifier;
        }
    }

    public interface IAddEditProfileWizard
    {
        WizardStep CurrentStep { get; set; }

        FeatureType FeatureType { get; set; }

        bool InProgress { get; set; }

        Task<SaveAsyncResults> SaveAsync(ProfileProperties profileProperties, CredentialFileType fileType);

        Task ChangeAwsExplorerConnectionAsync(ICredentialIdentifier credentialIdentifier);

        event EventHandler<CredentialsFileOpenedEventArgs> CredentialsFileOpened;

        void RecordAuthAddedConnectionsMetric(ActionResults actionResults, int newConnectionCount, IEnumerable<string> newEnabledConnections);
    }
}
