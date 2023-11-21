using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services
{
    public enum FeatureType
    {
        [Description("")]
        NotSet,

        [Description("AWS Explorer")]
        AwsExplorer,

        [Description("CodeWhisperer")]
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

        void RecordAuthAddConnectionMetric(ActionResults actionResults, CredentialSourceId credentialSourceId);

        void RecordAuthAddedConnectionsMetric(ActionResults actionResults, int newConnectionCount, IEnumerable<string> newEnabledConnections);
    }
}
