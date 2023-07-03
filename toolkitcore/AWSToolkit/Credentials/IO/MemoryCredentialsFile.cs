using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Credentials.IO
{
    /// <summary>
    /// Backing "file" store for MemoryCredentialFileReader/Writer.
    /// </summary>
    /// <remarks>
    /// Operations behave similarly to NetSdk/SharedCredentialsFile from AWSSDK.
    /// </remarks>
    internal class MemoryCredentialsFile : ICredentialProfileStore, ICredentialProfileSource
    {
        private readonly IDictionary<string, CredentialProfile> _profiles = new Dictionary<string, CredentialProfile>();

        #region Changed
        public event EventHandler<EventArgs> Changed;

        protected virtual void OnChanged(EventArgs e = null)
        {
            Changed?.Invoke(this, e ?? EventArgs.Empty);
        }
        #endregion

        #region RenameProfile
        private bool CanCopyOrRename(string fromProfileName, string toProfileName, bool force, out CredentialProfile fromProfile)
        {
            var notSameName = fromProfileName != toProfileName;
            var fromExists = _profiles.TryGetValue(fromProfileName, out fromProfile);
            var toExists = _profiles.ContainsKey(toProfileName);

            if (!force && toExists)
            {
                throw new ArgumentException($"Cannot perform operation as {toProfileName} already exists.");
            }

            return notSameName && fromExists;
        }

        public void RenameProfile(string oldProfileName, string newProfileName)
        {
            RenameProfile(oldProfileName, newProfileName, false);
        }

        public void RenameProfile(string oldProfileName, string newProfileName, bool force)
        {
            if (!CanCopyOrRename(oldProfileName, newProfileName, force, out var profile))
            {
                throw new ArgumentException($"Cannot rename profile {oldProfileName} to {newProfileName}.");
            }

            _profiles.Remove(oldProfileName);
            _profiles[newProfileName] = profile;
            OnChanged();
        }
        #endregion

        #region CopyProfile
        public void CopyProfile(string fromProfileName, string toProfileName)
        {
            CopyProfile(fromProfileName, toProfileName, false);
        }

        public void CopyProfile(string fromProfileName, string toProfileName, bool force)
        {
            if (!CanCopyOrRename(fromProfileName, toProfileName, force, out var fromProfile))
            {
                throw new ArgumentException($"Cannot copy profile {fromProfileName} to {toProfileName}.");
            }

            _profiles[toProfileName] = CopyProfile(toProfileName, fromProfile);
            OnChanged();
        }

        private CredentialProfile CopyProfile(string toProfileName, CredentialProfile fromProfile)
        {
            return new CredentialProfile(toProfileName, CopyProfileOptions(fromProfile.Options))
            {
                DefaultConfigurationModeName = fromProfile.DefaultConfigurationModeName,
                EC2MetadataServiceEndpoint = fromProfile.EC2MetadataServiceEndpoint,
                EC2MetadataServiceEndpointMode = fromProfile.EC2MetadataServiceEndpointMode,
                EndpointDiscoveryEnabled = fromProfile.EndpointDiscoveryEnabled,
                MaxAttempts = fromProfile.MaxAttempts,
                Region = fromProfile.Region,
                RetryMode = fromProfile.RetryMode,
                S3DisableMultiRegionAccessPoints = fromProfile.S3DisableMultiRegionAccessPoints,
                S3RegionalEndpoint = fromProfile.S3RegionalEndpoint,
                S3UseArnRegion = fromProfile.S3UseArnRegion,
                StsRegionalEndpoints = fromProfile.StsRegionalEndpoints,
                UseDualstackEndpoint = fromProfile.UseDualstackEndpoint,
                UseFIPSEndpoint = fromProfile.UseFIPSEndpoint
            };
        }

        private CredentialProfileOptions CopyProfileOptions(CredentialProfileOptions fromOptions)
        {
            return new CredentialProfileOptions()
            {
                AccessKey = fromOptions.AccessKey,
                CredentialProcess = fromOptions.CredentialProcess,
                CredentialSource = fromOptions.CredentialSource,
                EndpointName = fromOptions.EndpointName,
                ExternalID = fromOptions.ExternalID,
                MfaSerial = fromOptions.MfaSerial,
                RoleArn = fromOptions.RoleArn,
                RoleSessionName = fromOptions.RoleSessionName,
                SecretKey = fromOptions.SecretKey,
                SourceProfile = fromOptions.SourceProfile,
                SsoAccountId = fromOptions.SsoAccountId,
                SsoRegion = fromOptions.SsoRegion,
                SsoRoleName = fromOptions.SsoRoleName,
                SsoSession = fromOptions.SsoSession,
                SsoStartUrl = fromOptions.SsoStartUrl,
                Token = fromOptions.Token,
                UserIdentity = fromOptions.UserIdentity,
                WebIdentityTokenFile = fromOptions.WebIdentityTokenFile
            };
        }
        #endregion

        public List<string> ListProfileNames()
        {
            return _profiles.Keys.ToList();
        }

        public List<CredentialProfile> ListProfiles()
        {
            return _profiles.Values.ToList();
        }

        public bool TryGetProfile(string profileName, out CredentialProfile profile)
        {
            return _profiles.TryGetValue(profileName, out profile);
        }

        public void RegisterProfile(CredentialProfile profile)
        {
            if (!profile.CanCreateAWSCredentials)
            {
                throw new ArgumentException($"Unable to update profile {profile.Name}.  The CredentialProfile provided is not a valid profile.");
            }

            _profiles[profile.Name] = profile;
            OnChanged();
        }

        public void UnregisterProfile(string profileName)
        {
            _profiles.Remove(profileName);
            OnChanged();
        }
    }
}
