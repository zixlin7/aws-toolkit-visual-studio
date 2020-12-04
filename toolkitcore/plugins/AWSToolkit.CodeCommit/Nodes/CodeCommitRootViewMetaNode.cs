using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CodeCommit.Interface.Nodes;

namespace Amazon.AWSToolkit.CodeCommit.Nodes
{
    public class CodeCommitRootViewMetaNode : ServiceRootViewMetaNode, ICodeCommitRootViewMetaNode
    {
        public const string CodeCommit_ENDPOINT_LOOKUP = "CodeCommit";

        public CodeCommitRepositoryViewMetaNode CodeCommitRepositoryViewMetaNode => this.FindChild<CodeCommitRepositoryViewMetaNode>();

        public override string EndPointSystemName => CodeCommit_ENDPOINT_LOOKUP;

        public override ServiceRootViewModel CreateServiceRootModel(AccountViewModel account)
        {
            return new CodeCommitRootViewModel(account);
        }

        public override string MarketingWebSite => "https://aws.amazon.com/codecommit/";
    }
}
