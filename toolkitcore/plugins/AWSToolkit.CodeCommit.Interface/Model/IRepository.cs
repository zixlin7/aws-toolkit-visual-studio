using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Annotations;

namespace Amazon.AWSToolkit.CodeCommit.Interface.Model
{
    /// <summary>
    /// Wrapper around a CodeCommit repository. The repository may exist only
    /// on the remote or may have also been cloned locally.
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// Unique id for the repo. In practice, this is the repository's ARN.
        /// </summary>
        string UniqueId { get; }

        /// <summary>
        /// The folder root of the local clone. Null if the repo has not been cloned 
        /// locally or we have no persisted association to the local clone yet.
        /// </summary>
        string LocalFolder { get; }

        /// <summary>
        /// The URL to the CodeCommit remote.
        /// </summary>
        string RepositoryUrl { get; }

        /// <summary>
        /// The AWS credential account associated with the repository. To enable
        /// git operations against the repo the account should have associated
        /// service-specific credentials.
        /// </summary>
        AccountViewModel Account { get; }

        /// <summary>
        /// Tests whether service-specific credentials have been associated with the
        /// credential profile referenced by OwnerAccount. Service-specific credentials
        /// are required in order to perform git operations against the repository.
        /// </summary>
        bool HasServiceSpecificCredentials { get; }
    }
}
