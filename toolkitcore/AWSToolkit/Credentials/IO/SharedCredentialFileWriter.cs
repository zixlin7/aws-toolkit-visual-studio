using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.CredentialManagement.Internal;

namespace Amazon.AWSToolkit.Credentials.IO
{
    public class SharedCredentialFileWriter : ICredentialFileWriter
    {
        private readonly SharedCredentialsFile _sharedCredentialsFile;
        public string FilePath { get; set; }

        public SharedCredentialFileWriter(SharedCredentialsFile file)
        {
            _sharedCredentialsFile = file ?? new SharedCredentialsFile(FilePath);
        }

        public void CreateOrUpdateProfile(CredentialProfile profile)
        {
            EnsureUniqueKeyAssigned(profile);
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
