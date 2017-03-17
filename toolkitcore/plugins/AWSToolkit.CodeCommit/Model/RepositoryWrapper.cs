using Amazon.CodeCommit.Model;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    /// <summary>
    /// Wrapper around a CodeCommit repository object providing
    /// bindable metadata for UI purposes.
    /// </summary>
    public class RepositoryWrapper
    {

        public RepositoryWrapper(RepositoryMetadata repository)
        {
            RepositoryMetadata = repository;
        }

        public string Name => RepositoryMetadata.RepositoryName;
        public string Description => RepositoryMetadata.RepositoryDescription;

        public RepositoryMetadata RepositoryMetadata { get; }

        private RepositoryWrapper() { }
    }
}
