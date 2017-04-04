using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.CodeCommit.Model;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    /// <summary>
    /// Wrapper around a CodeCommit repository object providing
    /// bindable metadata for UI purposes.
    /// </summary>
    public class RepositoryWrapper : IRepository
    {
        public RepositoryWrapper(RepositoryMetadata repository)
        {
            RepositoryMetadata = repository;
        }

        public RepositoryWrapper(RepositoryWrapper source, string localFolder)
        {
            RepositoryMetadata = source.RepositoryMetadata;
            LocalFolder = localFolder;
        }

        public string Name => RepositoryMetadata.RepositoryName;

        public string Description => RepositoryMetadata.RepositoryDescription;

        public RepositoryMetadata RepositoryMetadata { get; }

        private RepositoryWrapper() { }

        #region IRepository

        public string UniqueId => RepositoryMetadata.RepositoryId;

        public string LocalFolder { get; private set; }

        public string RepositoryUrl => RepositoryMetadata.CloneUrlHttp;

        public bool HasServiceSpecificCredentials => false;

        #endregion
    }
}
