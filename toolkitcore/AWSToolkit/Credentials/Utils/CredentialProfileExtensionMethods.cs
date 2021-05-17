using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.CredentialManagement.Internal;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public static class CredentialProfileExtensionMethods
    {
        /// <summary>
        /// Creates <see cref="ProfileProperties"/> from <see cref="CredentialProfile"/>
        /// </summary>
        public static ProfileProperties AsProfileProperties(this CredentialProfile profile)
        {
            if (profile == null)
            {
                return null;
            }

            return new ProfileProperties()
            {
                Name = profile.Name,
                AccessKey = profile.Options?.AccessKey ?? string.Empty,
                SecretKey = profile.Options?.SecretKey ?? string.Empty,
                Token = profile.Options?.Token ?? string.Empty,
                CredentialProcess = profile.Options?.CredentialProcess ?? string.Empty,
                RoleArn = profile.Options?.RoleArn ?? string.Empty,
                MfaSerial = profile.Options?.MfaSerial ?? string.Empty,
                EndpointName = profile.Options?.EndpointName ?? string.Empty,
                Region = profile.Region?.SystemName ?? string.Empty,
                SsoAccountId = profile.Options?.SsoAccountId ?? string.Empty,
                SsoRegion = profile.Options?.SsoRegion ?? string.Empty,
                SsoRoleName = profile.Options?.SsoRoleName ?? string.Empty,
                SsoStartUrl = profile.Options?.SsoStartUrl ?? string.Empty,
                UniqueKey = GetUniqueKey(profile)
            };
        }

        private static string GetUniqueKey(CredentialProfile profile)
        {
            return CredentialProfileUtils.GetUniqueKey(profile);
        }
    }
}
