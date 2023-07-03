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
                CredentialSource = profile.Options?.CredentialSource ?? string.Empty,
                SourceProfile = profile.Options?.SourceProfile ?? string.Empty,
                Region = profile.Region?.SystemName ?? string.Empty,
                SsoSession = profile.Options?.SsoSession ?? string.Empty,
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


        public static CredentialProfile Clone(this CredentialProfile @this, string toProfileName)
        {
            return new CredentialProfile(toProfileName, @this.Options.Clone())
            {
                DefaultConfigurationModeName = @this.DefaultConfigurationModeName,
                EC2MetadataServiceEndpoint = @this.EC2MetadataServiceEndpoint,
                EC2MetadataServiceEndpointMode = @this.EC2MetadataServiceEndpointMode,
                EndpointDiscoveryEnabled = @this.EndpointDiscoveryEnabled,
                MaxAttempts = @this.MaxAttempts,
                Region = @this.Region,
                RetryMode = @this.RetryMode,
                S3DisableMultiRegionAccessPoints = @this.S3DisableMultiRegionAccessPoints,
                S3RegionalEndpoint = @this.S3RegionalEndpoint,
                S3UseArnRegion = @this.S3UseArnRegion,
                StsRegionalEndpoints = @this.StsRegionalEndpoints,
                UseDualstackEndpoint = @this.UseDualstackEndpoint,
                UseFIPSEndpoint = @this.UseFIPSEndpoint
            };
        }

        public static CredentialProfileOptions Clone(this CredentialProfileOptions @this)
        {
            return new CredentialProfileOptions()
            {
                AccessKey = @this.AccessKey,
                CredentialProcess = @this.CredentialProcess,
                CredentialSource = @this.CredentialSource,
                EndpointName = @this.EndpointName,
                ExternalID = @this.ExternalID,
                MfaSerial = @this.MfaSerial,
                RoleArn = @this.RoleArn,
                RoleSessionName = @this.RoleSessionName,
                SecretKey = @this.SecretKey,
                SourceProfile = @this.SourceProfile,
                SsoAccountId = @this.SsoAccountId,
                SsoRegion = @this.SsoRegion,
                SsoRoleName = @this.SsoRoleName,
                SsoSession = @this.SsoSession,
                SsoStartUrl = @this.SsoStartUrl,
                Token = @this.Token,
                UserIdentity = @this.UserIdentity,
                WebIdentityTokenFile = @this.WebIdentityTokenFile
            };
        }
    }
}
