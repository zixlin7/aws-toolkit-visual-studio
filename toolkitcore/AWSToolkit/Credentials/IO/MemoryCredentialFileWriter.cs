using System;

using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.CredentialManagement.Internal;

namespace Amazon.AWSToolkit.Credentials.IO
{
    internal class MemoryCredentialFileWriter : ICredentialFileWriter
    {
        private readonly MemoryCredentialsFile _file;

        public MemoryCredentialFileWriter(MemoryCredentialsFile file)
        {
            _file = file;
        }

        public void CreateOrUpdateProfile(CredentialProfile profile)
        {
            EnsureUniqueKeyAssigned(profile);

            if (!string.IsNullOrWhiteSpace(profile.Options.SsoSession))
            {
                var ssoSessionSectionName = ProfileName.CreateSsoSessionProfileName(profile.Options.SsoSession);

                // If sso-session section already exists in file...
                if (_file.TryGetProfile(ssoSessionSectionName, out var ssoSession))
                {
                    // Ensure it matches exactly
                    if (ssoSession.Options.SsoRegion != profile.Options.SsoRegion ||
                        ssoSession.Options.SsoStartUrl != profile.Options.SsoStartUrl)
                    {
                        throw new ArgumentException("Cannot save a sso-session with same name and different region/start URL.",
                            nameof(profile));
                    }
                }
                else
                {
                    // Else create sso-session section in file
                    _file.RegisterProfile(new CredentialProfile(ssoSessionSectionName,
                        new CredentialProfileOptions()
                        {
                            SsoRegion = profile.Options.SsoRegion,
                            SsoStartUrl = profile.Options.SsoStartUrl
                        }));
                }

                // Unlike SharedCredentialFileWriter.CreateOrUpdateProfile, leave the start URL and region in the profile
                // to simulate "SDK Hydrated" profiles in which the SDK automatically applies the sso-session values into
                // the profile returned.
            }

            _file.RegisterProfile(profile);
        }

        public void DeleteProfile(string profileName)
        {
            _file.UnregisterProfile(profileName);
        }

        public void EnsureUniqueKeyAssigned(CredentialProfile profile)
        {
            CredentialProfileUtils.EnsureUniqueKeyAssigned(profile, _file);
        }

        public void RenameProfile(string oldProfileName, string newProfileName)
        {
            _file.RenameProfile(oldProfileName, newProfileName);
        }
    }
}
