using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// Responsible for managing CRUD operations on credentials
    /// Determines the factory associated with given identifier to actually perform the CRUD operation
    /// </summary>
    public interface ICredentialSettingsManager
    {
        void CreateProfile(ICredentialIdentifier identifier, ProfileProperties properties);
        void RenameProfile(ICredentialIdentifier oldIdentifier, ICredentialIdentifier newIdentifier);
        void DeleteProfile(ICredentialIdentifier identifier);

        void UpdateProfile(ICredentialIdentifier identifier,
            ProfileProperties properties);

        ProfileProperties GetProfileProperties(ICredentialIdentifier identifier);
    }
}
