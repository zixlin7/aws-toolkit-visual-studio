using System;

using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.CodeCommit.Model;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    /// <summary>
    /// Wrapper around an existing CodeCommit repository object providing
    /// bindable metadata for UI purposes. The repository may exist locally
    /// or may only be remote.
    /// </summary>
    public class CodeCommitRepository : ICodeCommitRepository
    {
        public CodeCommitRepository(RepositoryMetadata repository)
        {
            RepositoryMetadata = repository;
        }

        public CodeCommitRepository(CodeCommitRepository source, string localFolder)
        {
            RepositoryMetadata = source.RepositoryMetadata;
            LocalFolder = localFolder;
        }

        public string Name => RepositoryMetadata?.RepositoryName;

        public string Description => RepositoryMetadata?.RepositoryDescription;

        public RepositoryMetadata RepositoryMetadata { get; }

        public string UniqueId => RepositoryMetadata.RepositoryId;

        public string LocalFolder { get; set; }

        public string RepositoryUrl => RepositoryMetadata.CloneUrlHttp;

        public DateTime? LastModifiedDate => RepositoryMetadata?.LastModifiedDate;
    }
}
