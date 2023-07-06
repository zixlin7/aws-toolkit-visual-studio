using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.Credentials.Core
{
    public interface IAwsConnectionManager
    {
        event EventHandler<ConnectionStateChangeArgs> ConnectionStateChanged;
        event EventHandler<ConnectionSettingsChangeArgs> ConnectionSettingsChanged;

        IIdentityResolver IdentityResolver { get; }
        ICredentialManager CredentialManager { get; }
        ConnectionState ConnectionState { get; }
        ToolkitRegion ActiveRegion { get; }
        ICredentialIdentifier ActiveCredentialIdentifier { get; }
        ToolkitCredentials ActiveCredentials { get;}
        string ActiveAccountId { get; }
        string ActiveAwsId { get; }

        void ChangeCredentialProvider(ICredentialIdentifier identifier);
        void ChangeRegion(ToolkitRegion region);
        void RefreshConnectionState();
        void ChangeConnectionSettings(ICredentialIdentifier identifier, ToolkitRegion region);
        Task<ConnectionState> ChangeConnectionSettingsAsync(ICredentialIdentifier credentialIdentifier,
            ToolkitRegion region, CancellationToken cancellationToken = default);
        bool IsValidConnectionSettings();
    }
}
