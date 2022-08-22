using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CodeCommit.Interface.Nodes;
using Amazon.AWSToolkit.Regions;
using Amazon.CodeCommit;

namespace Amazon.AWSToolkit.CodeCommit.Nodes
{
    public class CodeCommitRootViewMetaNode : ServiceRootViewMetaNode, ICodeCommitRootViewMetaNode
    {
        private static readonly string CodeCommitServiceName = new AmazonCodeCommitConfig().RegionEndpointServiceName;

        public CodeCommitRepositoryViewMetaNode CodeCommitRepositoryViewMetaNode => FindChild<CodeCommitRepositoryViewMetaNode>();

        public override string SdkEndpointServiceName => CodeCommitServiceName;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account, ToolkitRegion region)
        {
            return new CodeCommitRootViewModel(account, region);
        }

        public override string MarketingWebSite => "https://aws.amazon.com/codecommit/";
    }
}
