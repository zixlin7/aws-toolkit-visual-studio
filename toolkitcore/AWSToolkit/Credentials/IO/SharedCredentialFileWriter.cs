using System;

using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.CredentialManagement.Internal;

namespace Amazon.AWSToolkit.Credentials.IO
{
    public class SharedCredentialFileWriter : ICredentialFileWriter
    {
        private readonly SharedCredentialsFile _sharedCredentialsFile;

        public SharedCredentialFileWriter(SharedCredentialsFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            _sharedCredentialsFile = file;
        }

        public void CreateOrUpdateProfile(CredentialProfile profile)
        {
            EnsureUniqueKeyAssigned(profile);

            if (!string.IsNullOrWhiteSpace(profile.Options.SsoSession))
            {
                // If sso-session section already exists in file...
                if (_sharedCredentialsFile.TryGetSsoSession(profile.Options.SsoSession, out var ssoSession))
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
                    _sharedCredentialsFile.RegisterSsoSession(profile);
                }

                profile = profile.Clone(profile.Name);
                profile.Options.SsoRegion = null;
                profile.Options.SsoStartUrl = null;
            }

            _sharedCredentialsFile.RegisterProfile(profile);
        }

        public void RenameProfile(string oldProfileName, string newProfileName)
        {
            _sharedCredentialsFile.RenameProfile(oldProfileName, newProfileName);
        }

        public void DeleteProfile(string profileName)
        {
           _sharedCredentialsFile.UnregisterProfile(profileName);
        }

        /// <summary>
        /// Assigns a unique key if not present 
        /// </summary>
        /// <param name="profile"></param>
        public void EnsureUniqueKeyAssigned(CredentialProfile profile)
        {
            CredentialProfileUtils.EnsureUniqueKeyAssigned(profile, _sharedCredentialsFile);
        }
    }
}
