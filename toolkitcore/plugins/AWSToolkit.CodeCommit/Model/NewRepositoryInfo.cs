using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    internal class NewRepositoryInfo : INewCodeCommitRepositoryInfo
    {
        public AccountViewModel OwnerAccount { get; internal set; }
        public ToolkitRegion Region { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public string LocalFolder { get; internal set; }
        public GitIgnoreOption GitIgnore { get; internal set; }
    }
}
