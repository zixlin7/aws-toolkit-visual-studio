using System;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.Credentials.Core
{
    public interface IAwsConnectionManager
    {
        event EventHandler<ConnectionStateChangeArgs> ConnectionStateChanged;
        event EventHandler<ConnectionSettingsChangeArgs> ConnectionSettingsChanged;

        ICredentialManager CredentialManager { get; }
        ConnectionState ConnectionState { get; }
        ToolkitRegion ActiveRegion { get; }
        ICredentialIdentifier ActiveCredentialIdentifier { get; }
        AWSCredentials ActiveCredentials { get;}
        string ActiveAccountId { get; }

        void ChangeCredentialProvider(ICredentialIdentifier identifier);
        void ChangeRegion(ToolkitRegion region);
        void RefreshConnectionState();
        void ChangeConnectionSettings(ICredentialIdentifier identifier, ToolkitRegion region);
        bool IsValidConnectionSettings();
    }
}
