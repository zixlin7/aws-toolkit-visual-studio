using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// Provides CRUD operations on Credential Identifiers
    /// </summary>
    public interface ICredentialProfileProcessor
    {
        void CreateProfile(ICredentialIdentifier identifier, ProfileProperties properties);
        void RenameProfile(ICredentialIdentifier oldIdentifier, ICredentialIdentifier newIdentifier);
        void DeleteProfile(ICredentialIdentifier identifier);
        void UpdateProfile(ICredentialIdentifier identifier, ProfileProperties properties);
        ProfileProperties GetProfileProperties(ICredentialIdentifier identifier);
    }
}
