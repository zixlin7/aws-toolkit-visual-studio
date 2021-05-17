using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.CredentialManagement.Internal;

namespace Amazon.AWSToolkit.Credentials.IO
{
    public class SDKCredentialFileWriter: ICredentialFileWriter
    {
        private readonly NetSDKCredentialsFile _netSdkCredentialsFile;

        public SDKCredentialFileWriter(NetSDKCredentialsFile file)
        {
            _netSdkCredentialsFile = file ?? new NetSDKCredentialsFile();
        }

        public void CreateOrUpdateProfile(CredentialProfile profile)
        {
            EnsureUniqueKeyAssigned(profile);
            _netSdkCredentialsFile.RegisterProfile(profile);
        }

        public void RenameProfile(string oldProfileName, string newProfileName)
        {
            _netSdkCredentialsFile.RenameProfile(oldProfileName, newProfileName);
        }

        public void DeleteProfile(string profileName)
        {
            _netSdkCredentialsFile.UnregisterProfile(profileName);
        }

        /// <summary>
        /// Assigns a unique key to a profile if not present
        /// </summary>
        /// <param name="profile"></param>
        public void EnsureUniqueKeyAssigned(CredentialProfile profile)
        {
           CredentialProfileUtils.EnsureUniqueKeyAssigned(profile, _netSdkCredentialsFile);
        }
    }
}
