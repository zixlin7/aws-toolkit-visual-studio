using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Annotations;
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
        /// Clones the specified repository.
        /// </summary>
        /// <param name="repositoryUrl">The url of the repository to clone</param>
        /// <param name="destinationFolder">
        /// The destination folder for the cloned repository. This folder should not
        /// exist, or if it does, it must be empty.
        /// </param>
        /// <param name="credentials">
        /// The service-specific credentials for the operation. If the clone operation is
        /// delegated to Team Explorer, these credentials will be written to the OS
        /// credential store for later retrieval by Team Explorer. Once the clone operation
        /// is completed, we delete the stored credentials so we do not affect other sessions
        /// due to the credential store key being the regional CodeCommit endpoint, not the
        /// specific repo endpoint.
        /// </param>
        void Clone(string repositoryUrl, string destinationFolder, ServiceSpecificCredentials credentials);
    }
}
