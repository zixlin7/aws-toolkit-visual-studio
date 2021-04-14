using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.Credentials.Core
{
    public interface ICredentialManager
    {
        /// <summary>
        /// Event to indicate that the credential manager has been updated with the latest list of available credential profiles
        /// </summary>
        event EventHandler<EventArgs> CredentialManagerUpdated;
        ICredentialSettingsManager CredentialSettingsManager { get; }
        ICredentialIdentifier GetCredentialIdentifierById(string id);
        List<ICredentialIdentifier> GetCredentialIdentifiers();
        bool IsLoginRequired(ICredentialIdentifier identifier);
        AWSCredentials GetAwsCredentials(ICredentialIdentifier credentialIdentifier, ToolkitRegion region);
    }
}
