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
        bool Supports(ICredentialIdentifier credentialIdentifier, AwsConnectionType connectionType);
        AWSCredentials GetAwsCredentials(ICredentialIdentifier credentialIdentifier, ToolkitRegion region);
        ToolkitCredentials GetToolkitCredentials(ICredentialIdentifier credentialIdentifier, ToolkitRegion region);

        void Invalidate(ICredentialIdentifier credentialIdentifier);
    }

    public static class ICredentialManagerExtensionMethods
    {
        public static AWSCredentials GetAwsCredentials(this ICredentialManager @this, AwsConnectionSettings settings)
        {
            return @this.GetAwsCredentials(settings.CredentialIdentifier, settings.Region);
        }

        public static ToolkitCredentials GetToolkitCredentials(this ICredentialManager @this, AwsConnectionSettings settings)
        {
            return @this.GetToolkitCredentials(settings.CredentialIdentifier, settings.Region);
        }
    }
}
