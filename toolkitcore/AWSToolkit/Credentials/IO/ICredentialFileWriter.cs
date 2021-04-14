using System;
using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Credentials.IO
{
    /// <summary>
    /// Performs CRUD operations on credentials registered with a Credentials file
    /// </summary>
    public interface ICredentialFileWriter
    {
        void CreateOrUpdateProfile(CredentialProfile profile);
        void RenameProfile(string oldProfileName, string newProfileName);
        void DeleteProfile(string profileName);
        void EnsureUniqueKeyAssigned(CredentialProfile profile);
    }
}
