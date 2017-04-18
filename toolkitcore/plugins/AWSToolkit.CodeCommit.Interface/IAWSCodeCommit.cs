using System.Collections.Generic;
using System.IO.Packaging;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.CodeCommit.Interface
{
    /// <summary>
    /// General purpose interface onto the CodeCommit plugin
    /// </summary>
    public interface IAWSCodeCommit
    {
        /// <summary>
        /// Returns the CodeCommit plugin implementation of git services for the toolkit. This 
        /// implementation performs operations using LibGit2Sharp and CodeCommit, and bypasses
        /// the implementation based around Team Explorer that you get if you query for this
        /// interface on the VS shell provider.
        /// </summary>
        IAWSToolkitGitServices ToolkitGitServices { get; }

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
        /// Obtains and returns the service-specific credentials to enable git operations
        /// against a CodeCommit repo owned by the specified account.
        /// </summary>
        /// <param name="account">The account we want service specific credentials for.</param>
        /// <param name="region">
        /// Used if we attempt to create credentials, to construct the necessary IAM client.
        /// </param>
        /// <param name="ignoreCurrent">
        /// Forces bypass of any credentials already associated with the account. Used when we
        /// want to force an update of the credentials by obtaining new ones.
        /// </param>
        /// <returns></returns>
        ServiceSpecificCredentials ObtainGitCredentials(AccountViewModel account, 
                                                        RegionEndPointsManager.RegionEndPoints region,
                                                        bool ignoreCurrent);

        /// <summary>
        /// Prompts the user to select a repository to clone. The account and initial region
        /// are usually seeded from the current AWS Explorer bindings.
        /// </summary>
        /// <param name="account">The account for the repositories to list for selection.</param>
        /// <param name="initialRegion">Initial region selection or null.</param>
        /// <param name="defaultCloneFolderRoot">Suggested folder for the cloned repository, or null.</param>
        /// <returns>Null if the user cancels selection, otherwise details of the repository to clone.</returns>
        ICodeCommitRepository PromptForRepositoryToClone(AccountViewModel account, 
                                                         RegionEndPointsManager.RegionEndPoints initialRegion, 
                                                         string defaultCloneFolderRoot);

        /// <summary>
        /// Prompts the user to fill out the data necessary to create a new repository hosted
        /// in CodeCommit, returning details of the newly created repository so that it can be
        /// cloned locally.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="initialRegion"></param>
        /// <param name="defaultFolderRoot"></param>
        /// <returns>Details of the repository to be created.</returns>
        INewCodeCommitRepositoryInfo PromptForRepositoryToCreate(AccountViewModel account, 
                                                                 RegionEndPointsManager.RegionEndPoints initialRegion,
                                                                 string defaultFolderRoot);

        /// <summary>
        /// Prompts the user to stores a set of newly generated credentials to disk.
        /// </summary>
        /// <param name="generatedCredentials">The details of the generated credentials returned by IAM.</param>
        /// <returns>The path and name of the file holding the stored credentials.</returns>
        string PromptToSaveGeneratedCredentials(ServiceSpecificCredential generatedCredentials, string msg = null);

        /// <summary>
        /// Test if a local repository has a remote pointing at a CodeCommit endpoint.
        /// </summary>
        /// <param name="repoPath">The local repository path, which should exist.</param>
        /// <returns>True if a CodeCommit endpoint is found in the repo's remotes collection.</returns>
        bool IsCodeCommitRepository(string repoPath);

        /// <summary>
        /// Returns the host region for a repository, parsed from the remote url.
        /// </summary>
        /// <param name="repoPath"></param>
        /// <returns></returns>
        string GetRepositoryRegion(string repoPath);

        /// <summary>
        /// Queries for and wraps an ICodeCommitRepository instance around repositories found on
        /// disk, if they belong to the supplied account.
        /// </summary>
        /// <param name="account">The account we *expect* owns this set of repositories.</param>
        /// <param name="pathsToRepositories">Collection of one or more local paths to the repositories.</param>
        /// <returns>
        /// Collection of wrappers around the found repositories. Repositories we failed to
        /// process are dropped on the floor.
        /// </returns>
        IEnumerable<ICodeCommitRepository> GetRepositories(AccountViewModel account, 
                                                           IEnumerable<string> pathsToRepositories);

        /// <summary>
        /// Returns the wrapped metadata for a CodeCommit repository.
        /// </summary>
        /// <param name="repositoryName">The name of the repository</param>
        /// <param name="account">The owning account</param>
        /// <param name="region">The region hosting the repository</param>
        /// <returns></returns>
        ICodeCommitRepository GetRepository(string repositoryName,
                                            AccountViewModel account,
                                            RegionEndPointsManager.RegionEndPoints region);

        /// <summary>
        /// Forms the url to enable the user to browse the repo content in the AWS console.
        /// </summary>
        /// <param name="repoPath"></param>
        /// <returns></returns>
        string GetConsoleBrowsingUrl(string repoPath);

        /// <summary>
        /// Stages and commits a set of files.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="commitMessage"></param>
        /// <returns></returns>
        bool StageAndCommit(IEnumerable<string> files, string commitMessage);

        /// <summary>
        /// Pushes the latest commits to the specified remote.
        /// </summary>
        /// <param name="remote"></param>
        /// <returns></returns>
        bool Push(string remote);
    }
}
