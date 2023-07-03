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
