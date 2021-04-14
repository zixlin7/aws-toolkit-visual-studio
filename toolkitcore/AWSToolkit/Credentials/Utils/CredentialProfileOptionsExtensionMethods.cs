using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public static class CredentialProfileOptionsExtensionMethods
    {
        public static bool ContainsSsoProperties(this CredentialProfileOptions profileOptions)
        {
            // If any SSO field is filled in, the profile is considered an SSO based profile
            if (!string.IsNullOrWhiteSpace(profileOptions.SsoAccountId) ||
                !string.IsNullOrWhiteSpace(profileOptions.SsoRegion) ||
                !string.IsNullOrWhiteSpace(profileOptions.SsoRoleName) ||
                !string.IsNullOrWhiteSpace(profileOptions.SsoStartUrl))
            {
                return true;
            }

            return false;
        }
    }
}
