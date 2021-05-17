namespace Amazon.AWSToolkit.Credentials.Utils
{
    public static class ProfilePropertiesExtensionMethods
    {
        /// <summary>
        /// Determines the generalized type of credentials represented by the given profile properties.
        /// </summary>
        public static CredentialType GetCredentialType(this ProfileProperties properties)
        {
            if (properties == null) { return CredentialType.Undefined; }

            // If any SSO field is filled in, the profile is considered an SSO based profile
            if (!string.IsNullOrWhiteSpace(properties.SsoAccountId) ||
                !string.IsNullOrWhiteSpace(properties.SsoRegion) ||
                !string.IsNullOrWhiteSpace(properties.SsoRoleName) ||
                !string.IsNullOrWhiteSpace(properties.SsoStartUrl))
            {
                return CredentialType.SsoProfile;
            }

            if (!string.IsNullOrWhiteSpace(properties.EndpointName))
            {
                return CredentialType.AssumeSamlRoleProfile;
            }

            if (!string.IsNullOrWhiteSpace(properties.RoleArn))
            {
                if (!string.IsNullOrWhiteSpace(properties.MfaSerial))
                {
                    return CredentialType.AssumeMfaRoleProfile;
                }
                else
                {
                    return CredentialType.AssumeRoleProfile;
                }
            }

            if (!string.IsNullOrWhiteSpace(properties.Token))
            {
                return CredentialType.StaticSessionProfile;
            }

            if (!string.IsNullOrWhiteSpace(properties.AccessKey)) { return CredentialType.StaticProfile; }

            if (!string.IsNullOrWhiteSpace(properties.CredentialProcess))
            {
                return CredentialType.CredentialProcessProfile;
            }

            return CredentialType.Unknown;
        }
    }
}
