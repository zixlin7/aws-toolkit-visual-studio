using System.IO;

namespace Amazon.AWSToolkit.CodeCommit.Interface.Model
{
    /// <summary>
    /// Wrapper around a CodeCommit repository. The repository may exist only
    /// on the remote or may have also been cloned locally.
    /// </summary>
    public interface ICodeCommitRepository
    {
        /// <summary>
        /// Unique id for the repo.
        /// </summary>
        string UniqueId { get; }

        /// <summary>
        /// The folder root of the repository. If this is a clone operation the
        /// folder should not exist (or if it does, it must be empty).
        /// </summary>
        string LocalFolder { get; set; }

        /// <summary>
        /// The URL to the CodeCommit remote.
        /// </summary>
        string RepositoryUrl { get; }

        /// <summary>
        /// The name of the repository.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The user-supplied description of the repository, if any.
        /// </summary>
        string Description { get; }
    }
}
