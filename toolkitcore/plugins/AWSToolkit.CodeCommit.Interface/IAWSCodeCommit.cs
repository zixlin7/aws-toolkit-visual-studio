using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.AWSToolkit.Shared;
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
        /// Prompts the user to select a repository to clone. The account and initial region
        /// are usually seeded from the current AWS Explorer bindings.
        /// </summary>
        /// <param name="account">The account for the repositories to list for selection.</param>
        /// <param name="initialRegion">Initial region selection or null.</param>
        /// <param name="defaultCloneFolderRoot">Suggested folder for the cloned repository, or null.</param>
        /// <returns>Null if the user cancels selection, otherwise details of the repository to clone.</returns>
        IRepository SelectRepositoryToClone(AccountViewModel account, 
                                            RegionEndPointsManager.RegionEndPoints initialRegion, 
                                            string defaultCloneFolderRoot);

        /// <summary>
        /// Returns an implementation for accessing git operations against CodeCommit repositories. 
        /// If running inside Visual Studio this returns the package level implementation that will 
        /// forward operations onto Team Explorer otherwise the CodeCommit plugin implementation is 
        /// returned.
        /// </summary>
        IAWSToolkitGitServices ToolkitGitServices { get; }
    }
}
