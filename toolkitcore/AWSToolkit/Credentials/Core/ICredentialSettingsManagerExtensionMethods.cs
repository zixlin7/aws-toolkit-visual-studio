using System;

using Amazon.AWSToolkit.Credentials.Utils;

using log4net;

namespace Amazon.AWSToolkit.Credentials.Core
{
    public static class ICredentialSettingsManagerExtensionMethods
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ICredentialSettingsManagerExtensionMethods));

        /// <summary>
        /// Using a settings manager, determines the Credential type for a specified credential id.
        /// If anything unexpected occurs, <see cref="CredentialType.Unknown"/> is returned.
        /// </summary>
        public static CredentialType GetCredentialType(this ICredentialSettingsManager credentialSettingsManager, ICredentialIdentifier credentialId)
        {
            try
            {
                if (credentialId == null)
                {
                    return CredentialType.Undefined;
                }

                return credentialSettingsManager.GetProfileProperties(credentialId).GetCredentialType();
            }
            catch (Exception e)
            {
                Logger.Error($"Error looking up credential type for {credentialId?.Id}", e);
                return CredentialType.Unknown;
            }
        }
    }
}
