using System;
using System.Collections.Generic;
using System.IO;

using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.Internal.Util;

namespace Amazon.AWSToolkit.Credentials.IO
{
    internal static class ExtensionMethods
    {
        internal const string _configFileName = "config";

        internal const string _ssoSessionSectionName = "sso-session";

        internal const string _ssoRegionPropertyName = "sso_region";

        internal const string _ssoStartUrlPropertyName = "sso_start_url";

        // Inspired by SharedCredentialsFile.Refresh.
        // https://github.com/aws/aws-sdk-net/blob/master/sdk/src/Core/Amazon.Runtime/CredentialManagement/SharedCredentialsFile.cs
        private static string GetSharedConfigFilePath(this SharedCredentialsFile @this)
        {
            // Re-check if they set an explicit config file path, use that if it's set
            var awsConfigEnvironmentPath = Environment.GetEnvironmentVariable(SharedCredentialsFile.SharedConfigFileEnvVar);
            if (!string.IsNullOrEmpty(awsConfigEnvironmentPath))
            {
                return awsConfigEnvironmentPath;
            }

            // config file will be in the same location as the credentials file and no env vars are set.
            // Just return the path, if the caller cares, they can figure out if it exists or not
            return Path.Combine(Path.GetDirectoryName(@this.FilePath), _configFileName);
        }

        /// <summary>
        /// Returns a ProfileIniFile of the shared config file.
        /// </summary>
        /// <param name="this">The SharedCredentialsFile to retrieve the config file for.</param>
        /// <returns>A ProfileIniFile instance of the config file.</returns>
        internal static ProfileIniFile GetSharedConfigFile(this SharedCredentialsFile @this)
        {
            // Second parameter profileMarkerRequired is required to be true for config files, but not for general ini files.
            return new ProfileIniFile(@this.GetSharedConfigFilePath(), true);
        }

        private static void ThrowOnNullOrWhiteSpace(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{name} must have value set.", name);
            }
        }

        internal static void RegisterSsoSession(this SharedCredentialsFile @this, CredentialProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var options = profile.Options;

            ThrowOnNullOrWhiteSpace(nameof(options.SsoSession), options.SsoSession);
            ThrowOnNullOrWhiteSpace(nameof(options.SsoRegion), options.SsoRegion);
            ThrowOnNullOrWhiteSpace(nameof(options.SsoStartUrl), options.SsoStartUrl);

            // Only sso_start_url and sso_region supported in sso-session sections for IAM Identity Center
            // Legacy profiles don't support sso_session sections, all Sso* keys defined directly in profile.
            var properties = new SortedDictionary<string, string>()
            {
                { _ssoRegionPropertyName, options.SsoRegion },
                { _ssoStartUrlPropertyName, options.SsoStartUrl }
            };

            var configFile = @this.GetSharedConfigFile();
            configFile.EnsureSectionExists(ProfileName.CreateSsoSessionProfileName(options.SsoSession));
            configFile.EditSection(options.SsoSession, true, properties); // Section must already exist to edit sso-session
            configFile.Persist();
        }

        internal static bool TryGetSsoSession(this SharedCredentialsFile @this, string ssoSessionName, out CredentialProfile profile)
        {
            ThrowOnNullOrWhiteSpace(nameof(ssoSessionName), ssoSessionName);
            return TryGetSsoSession(@this.GetSharedConfigFile(), ssoSessionName, out profile);
        }

        private static bool TryGetSsoSession(this ProfileIniFile @this, string ssoSessionName, out CredentialProfile profile)
        {
            profile = null;

            if (@this.TryGetSection(ssoSessionName, true, out var properties))
            {
                properties.TryGetValue(_ssoRegionPropertyName, out var ssoRegion);
                properties.TryGetValue(_ssoStartUrlPropertyName, out var ssoStartUrl);

                profile = new CredentialProfile(ssoSessionName, new CredentialProfileOptions()
                {
                    SsoRegion = ssoRegion,
                    SsoStartUrl = ssoStartUrl
                });
            }

            return profile != null;
        }
    }
}
