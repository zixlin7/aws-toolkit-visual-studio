using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.CodeCommit.Interface
{
    /// <summary>
    /// General purpose interface onto the CodeCommit plugin
    /// </summary>
    public interface IAWSCodeCommit
    {
        /// <summary>
        /// Returns the key used to identify a set of service-specific credentials
        /// as belonging to AWS CodeCommit
        /// </summary>
        string ServiceSpecificCredentialsStorageName { get; }

        /// <summary>
        /// Persists a set of service-specific credentials for CodeCommit, associating them
        /// with an existing AWS credentials profile via the unique artifact id.
        /// </summary>
        /// <param name="profileArtifactsId">The unique id associated with the credential profile</param>
        /// <param name="userName">The user name for the credentials</param>
        /// <param name="password">The associated password for the user</param>
        void AssociateCredentialsWithProfile(string profileArtifactsId, string userName, string password);

        /// <summary>
        /// Retrieves the service-specific credentials for CodeCommit associated with an AWS
        /// credentials profile (if any) via the profile's unique artifact id.
        /// </summary>
        /// <param name="profileArtifactsId">The unique id associated with the credential profile</param>
        /// <returns>Credentials data if available, or null</returns>
        ServiceSpecificCredentials CredentialsForProfile(string profileArtifactsId);

        /// <summary>
        /// Displays a dialog containing a list of repositories for the specified account. The chosen
        /// repository is then cloned locally. If the account does not have associated service-specific
        /// credentials for CodeCommit the user is prompted to supply them.
        /// </summary>
        /// <param name="account"></param>
        /// <returns>True if the command succeeded.</returns>
        bool CloneRepository(AccountViewModel account);

        /// <summary>
        /// Makes a local clone of the specified repository.
        /// </summary>
        /// <param name="credentials">Service specific credentials appropriate for the repository</param>
        /// <param name="repositoryUrl">Http(s) url of the repository to clone</param>
        /// <param name="localFolder">Folder to clone into (does not need to exist but if it does it should be empty)</param>
        /// <returns>True if the command succeeded.</returns>
        bool CloneRepository(ServiceSpecificCredentials credentials, string repositoryUrl, string localFolder);
    }
}
