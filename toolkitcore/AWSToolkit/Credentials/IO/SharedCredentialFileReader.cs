using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.Internal.Util;

namespace Amazon.AWSToolkit.Credentials.IO
{
    public class SharedCredentialFileReader : CredentialFileReader
    {
        private const string DefaultProfileName = "default";
        private const string ConfigFileName = "config";
        private readonly SharedCredentialsFile _sharedCredentialsFile;
        private ProfileIniFile _credentialFile;
        private ProfileIniFile _configFile;

        public string FilePath { get; set; }

        public static readonly Dictionary<string, string> PropertyMapping =
            new Dictionary<string, string>()
            {
                {ProfilePropertyConstants.AccessKey, "aws_access_key_id"},
                {ProfilePropertyConstants.CredentialSource, "credential_source"},
                {ProfilePropertyConstants.EndpointName, null},
                {ProfilePropertyConstants.EndpointUrl, null},
                {ProfilePropertyConstants.ExternalID, "external_id"},
                {ProfilePropertyConstants.MfaSerial, "mfa_serial"},
                {ProfilePropertyConstants.RoleArn, "role_arn"},
                {ProfilePropertyConstants.RoleSessionName, "role_session_name"},
                {ProfilePropertyConstants.SecretKey, "aws_secret_access_key"},
                {ProfilePropertyConstants.SourceProfile, "source_profile"},
                {ProfilePropertyConstants.Token, "aws_session_token"},
                {ProfilePropertyConstants.UserIdentity, null},
                {ProfilePropertyConstants.CredentialProcess, "credential_process"},
                {ProfilePropertyConstants.WebIdentityTokenFile, "web_identity_token_file"},
                {ProfilePropertyConstants.Services, null},
                {ProfilePropertyConstants.SsoAccountId, "sso_account_id"},
                {ProfilePropertyConstants.SsoRegion, "sso_region"},
                {ProfilePropertyConstants.SsoRegistrationScopes, "sso_registration_scopes"},
                {ProfilePropertyConstants.SsoRoleName, "sso_role_name"},
                {ProfilePropertyConstants.SsoSession, "sso_session"},
                {ProfilePropertyConstants.SsoStartUrl, "sso_start_url"}
            };
  
        public SharedCredentialFileReader(SharedCredentialsFile file)
        {
            _sharedCredentialsFile = file ?? new SharedCredentialsFile(FilePath);
        }

        public override void Load()
        {
            LoadCredentialFiles();
            ProfileNames = ListAllProfileNames();
        }

        public override CredentialProfileOptions GetCredentialProfileOptions(string profileName)
        {
            if (TryGetSection(profileName, out var properties))
            {
                return new ProfilePropertyMapping(PropertyMapping).ExtractProfileOptions(properties);
            }

            return null;
        }

        protected override ICredentialProfileStore GetProfileStore()
        {
            return _sharedCredentialsFile ??
                   throw new NullReferenceException(
                       "SharedCredentialFile object reference is not set to an instance of an object");
        }

        private List<string> ListAllProfileNames()
        {
            var profileNames = new HashSet<string>();
            if (_credentialFile != null)
            {
                profileNames.UnionWith(_credentialFile.ListSectionNames());
            }
            if (_configFile != null)
            {
                profileNames.UnionWith(_configFile.ListSectionNames());
            }

            return profileNames.ToList();
        }

        private void LoadCredentialFiles()
        {
            if (File.Exists(_sharedCredentialsFile.FilePath))
            {
                _credentialFile = new ProfileIniFile(_sharedCredentialsFile.FilePath, false);
            }

            var configPath = Path.Combine(Path.GetDirectoryName(_sharedCredentialsFile.FilePath), ConfigFileName);
            if (File.Exists(configPath))
            {
                _configFile = new ProfileIniFile(configPath, true);
            }
        }

        /// <summary>
        /// Try to get a profile that may be partially in the credentials file and partially in the config file.
        /// If there are identically named properties in both files, the properties in the credentials file take precedence.
        /// </summary>
        /// <param name="sectionName"></param>
        /// <param name="iniProperties"></param>
        /// <returns></returns>
        private bool TryGetSection(string sectionName, out Dictionary<string, string> iniProperties)
        {
            //check to ensure load call is made before attempting this call
            if (ProfileNames == null)
            {
                iniProperties = null;
                return false;
            }

            var isSsoSession = ProfileName.IsSsoSession(sectionName);
            if (isSsoSession)
            {
                sectionName = ProfileName.GetSsoSessionFromProfileName(sectionName);
            }

            Dictionary<string, string> credentialsProperties = null;
            Dictionary<string, string> configProperties = null;

            var hasCredentialsProperties = false;

            // Spec: SSO session sections in the credentials file are silently dropped
            if (!isSsoSession && _credentialFile != null)
            {
                hasCredentialsProperties = _credentialFile.TryGetSection(sectionName, out credentialsProperties);
            }
            var hasConfigProperties = false;
            if (_configFile != null)
            {
                _configFile.ProfileMarkerRequired = sectionName != DefaultProfileName;
                hasConfigProperties = _configFile.TryGetSection(sectionName, isSsoSession, out configProperties);
            }

            if (hasConfigProperties)
            {
                iniProperties = configProperties;
                if (hasCredentialsProperties)
                {
                    // Add all the properties from the credentials file.
                    // If a property exits in both, the one from the credentials
                    // file takes precedence and overwrites the one from
                    // the config file.
                    foreach (var pair in credentialsProperties)
                    {
                        iniProperties[pair.Key] = pair.Value;
                    }
                }

                return true;
            }

            iniProperties = credentialsProperties;
            return hasCredentialsProperties;
        }
    }
}
