using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.Shared
{
    /// <summary>
    /// Details of a new repository a user wants to have created.
    /// </summary>
    public interface INewCodeCommitRepositoryInfo
    {
        /// <summary>
        /// The account that will own the new repository.
        /// </summary>
        AccountViewModel OwnerAccount { get; }

        /// <summary>
        /// The region in which the repository will be created.
        /// </summary>
        ToolkitRegion Region { get; }

        /// <summary>
        /// The name for the repository.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Optional description for the repository.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The local folder that will contain the new repository content.
        /// </summary>
        string LocalFolder { get; }

        /// <summary>
        /// User election for .gitignore file handling
        /// </summary>
        GitIgnoreOption GitIgnore { get; }
    }
}
