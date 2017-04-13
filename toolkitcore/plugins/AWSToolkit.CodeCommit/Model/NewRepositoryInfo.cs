using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    internal class NewRepositoryInfo : INewCodeCommitRepositoryInfo
    {
        public AccountViewModel OwnerAccount { get; internal set; }
        public RegionEndPointsManager.RegionEndPoints Region { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public string LocalFolder { get; internal set; }
    }
}
