using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public static class CredentialProfileOptionsExtensionMethods
    {
        public static bool IsResolvedWithSso(this CredentialProfileOptions profileOptions)
        {
            // Spec: If either the sso_account_id or sso_role_name configurations values are present
            // the profile MUST be resolved by the SSO credential provider.
            return !string.IsNullOrWhiteSpace(profileOptions.SsoAccountId) ||
                   !string.IsNullOrWhiteSpace(profileOptions.SsoRoleName);
        }

        public static bool IsResolvedWithTokenProvider(this CredentialProfileOptions profileOptions)
        {
            // Spec : If the sso_session configuration value is present, the profile MUST be resolved by the SSO token provider.
            // (note: this is secondary to the spec in IsResolvedWithSso)
            return !profileOptions.IsResolvedWithSso() && profileOptions.ReferencesSsoSessionProfile();
        }

        public static bool ReferencesSsoSessionProfile(this CredentialProfileOptions profileOptions) =>
            !string.IsNullOrWhiteSpace(profileOptions.SsoSession);
    }
}
