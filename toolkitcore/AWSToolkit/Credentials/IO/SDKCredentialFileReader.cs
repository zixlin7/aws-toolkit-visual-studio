using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.Internal.Settings;
using Amazon.Util.Internal;

namespace Amazon.AWSToolkit.Credentials.IO
{
    public class SDKCredentialFileReader : CredentialFileReader
    {
        private readonly NetSDKCredentialsFile _netSdkCredentialsFile;
        private readonly NamedSettingsManager _manager;

        public static readonly Dictionary<string, string> PropertyMapping =
            new Dictionary<string, string>()
            {
                {ProfilePropertyConstants.AccessKey, SettingsConstants.AccessKeyField},
                {ProfilePropertyConstants.CredentialSource, SettingsConstants.CredentialSourceField},
                {ProfilePropertyConstants.EndpointName, SettingsConstants.EndpointNameField},
                {ProfilePropertyConstants.ExternalID, SettingsConstants.ExternalIDField},
                {ProfilePropertyConstants.MfaSerial, SettingsConstants.MfaSerialField},
                {ProfilePropertyConstants.RoleArn, SettingsConstants.RoleArnField},
                {ProfilePropertyConstants.RoleSessionName, SettingsConstants.RoleSessionName},
                {ProfilePropertyConstants.SecretKey, SettingsConstants.SecretKeyField},
                {ProfilePropertyConstants.SourceProfile, SettingsConstants.SourceProfileField},
                {ProfilePropertyConstants.Token, SettingsConstants.SessionTokenField},
                {ProfilePropertyConstants.UserIdentity, SettingsConstants.UserIdentityField},
                {ProfilePropertyConstants.CredentialProcess, SettingsConstants.CredentialProcess},
                {ProfilePropertyConstants.WebIdentityTokenFile, SettingsConstants.WebIdentityTokenFile},
                {ProfilePropertyConstants.SsoAccountId, "sso_account_id"},
                {ProfilePropertyConstants.SsoRegion, "sso_region"},
                {ProfilePropertyConstants.SsoRoleName, "sso_role_name"},
                {ProfilePropertyConstants.SsoSession, "sso_session"},
                {ProfilePropertyConstants.SsoStartUrl, "sso_start_url"}
            };

        public SDKCredentialFileReader(NetSDKCredentialsFile file) : this(file, null)
        {
        }

        public SDKCredentialFileReader(NetSDKCredentialsFile file, NamedSettingsManager manager)
        {
            _netSdkCredentialsFile = file ?? new NetSDKCredentialsFile();
            _manager = manager ?? new NamedSettingsManager(SettingsConstants.RegisteredProfiles);
        }

        public override void Load()
        {
            ProfileNames = _manager.ListObjectNames();
        }

        public override CredentialProfileOptions GetCredentialProfileOptions(string profileName)
        {
            if (_manager.TryGetObject(profileName, out var uniqueKeyStr, out var properties))
            {
                return new ProfilePropertyMapping(PropertyMapping).ExtractProfileOptions(properties);
            }

            return null;
        }

        protected override ICredentialProfileStore GetProfileStore()
        {
            return _netSdkCredentialsFile ?? throw new NullReferenceException("NetSDKCredentialFile object reference is not set to an instance of an object");
        }
    }
}
