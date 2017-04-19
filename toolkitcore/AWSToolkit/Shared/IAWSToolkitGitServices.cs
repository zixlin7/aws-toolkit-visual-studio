using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.Shared
{
    /// <summary>
    /// Interface onto git operations exposed as services within our
    /// VS package and the plugins it hosts. If running in a system with
    /// Team Explorer integration, we forward operation calls through this
    /// interface to Team Explorer otherwise we attempt to satisfy them
    /// using the CodeCommit plugin, if available.
    /// </summary>
    public interface IAWSToolkitGitServices
    {
        /// <summary>
        /// Service provider in the host VS shell, for accessing Team Explorer services.
        /// </summary>
        IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Clones the specified repository.
        /// </summary>
        /// <param name="credentials">
        /// The service-specific credentials for the operation. If the clone operation is
        /// delegated to Team Explorer, these credentials will be written to the OS
        /// credential store for later retrieval by Team Explorer. Once the clone operation
        /// is completed, we delete the stored credentials so we do not affect other sessions
        /// due to the credential store key being the regional CodeCommit endpoint, not the
        /// specific repo endpoint.
        /// </param>
        /// <param name="repositoryUrl">The url of the repository to clone</param>
        /// <param name="destinationFolder">
        /// The destination folder for the cloned repository. This folder should not
        /// exist, or if it does, it must be empty.
        /// </param>
        Task CloneAsync(ServiceSpecificCredentials credentials,
                        string repositoryUrl, 
                        string destinationFolder);

        /// <summary>
        /// Creates a new repository for the account in the specified region and clones
        /// the repository locally. An optional callback is then performed to populate
        /// the empty repository with content that is then committed and pushed back as
        /// the initial commit. Git credentials to perform the clone and commit operations
        /// will be retrieved from service specific credentials that have already been 
        /// attached to the supplied account.
        /// </summary>
        /// <param name="newRepositoryInfo">
        /// Details of the repository to be created.
        /// </param>
        /// <param name="autoCloneNewRepository">
        /// If true, the new repo is cloned locally after creation.
        /// </param>
        /// <param name="contentPopulationCallback">
        /// Optional callback to populate initial content into the new repo after it has been
        /// cloned locally. The content posted into the repository folder by the callback will 
        /// then be committed to the repo as 'initial commit' and pushed to the remote.
        /// </param>
        /// <returns>repository object castable to ICodeCommitRepository</returns>
        Task CreateAsync(INewCodeCommitRepositoryInfo newRepositoryInfo, 
                         bool autoCloneNewRepository, 
                         AWSToolkitGitCallbackDefinitions.PostCloneContentPopulationCallback contentPopulationCallback);
    }

    public abstract class AWSToolkitGitCallbackDefinitions
    {
        /// <summary>
        /// Callback to populate initial content into a new repository after it
        /// has been cloned locally. The content posted into the repository folder by
        /// the callback will then be committed to the repo as 'initial commit' and pushed
        /// to the remote.
        /// </summary>
        /// <param name="repositoryFolder"></param>
        /// <returns>
        /// The collection of files that were added to the working folder.
        /// </returns>
        public delegate IEnumerable<string> PostCloneContentPopulationCallback(string repositoryFolder);
    }
}
