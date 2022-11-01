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
        public static CredentialType GetCredentialType(this ICredentialSettingsManager @this, ICredentialIdentifier credentialIdentifier)
        {
            try
            {
                if (credentialIdentifier == null)
                {
                    return CredentialType.Undefined;
                }

                if (@this == null)
                {
                    return CredentialType.Unknown;
                }

                return @this.GetProfileProperties(credentialIdentifier).GetCredentialType();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error looking up credential type for {credentialIdentifier?.DisplayName}", ex);
                return CredentialType.Unknown;
            }
        }

        /// <summary>
        /// Returns the unique key, if any, from profile properties.
        /// </summary>
        /// <param name="this">Extension method applies to this.</param>
        /// <param name="credentialIdentifier">Credential identifier for which to obtain the unique key.</param>
        /// <returns>The unique key if found, otherwise null.</returns>
        public static string GetUniqueKey(this ICredentialSettingsManager @this, ICredentialIdentifier credentialIdentifier)
        {
            try
            {
                return @this.GetProfileProperties(credentialIdentifier).UniqueKey;
            }
            catch (Exception ex)
            {
                Logger.Error($"No profile properties found for {credentialIdentifier?.DisplayName}.", ex);
                return null;
            }
        }
    }
}
