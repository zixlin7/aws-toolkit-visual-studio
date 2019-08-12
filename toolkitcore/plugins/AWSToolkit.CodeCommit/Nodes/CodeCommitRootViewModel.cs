using System.Collections.Generic;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;
using Amazon.Runtime;
using Amazon.AWSToolkit.CodeCommit.Interface.Nodes;
using log4net;

namespace Amazon.AWSToolkit.CodeCommit.Nodes
{
    public class CodeCommitRootViewModel : ServiceRootViewModel, ICodeCommitRootViewModel
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(CodeCommitRootViewModel));

        readonly CodeCommitRootViewMetaNode _metaNode;

        public CodeCommitRootViewModel(AccountViewModel accountViewModel)
            : base(accountViewModel.MetaNode.FindChild<CodeCommitRootViewMetaNode>(), accountViewModel, "AWS CodeCommit")
        {
            this._metaNode = base.MetaNode as CodeCommitRootViewMetaNode;
        }

        public override string ToolTip => "AWS CodeCommit";

        protected override string IconName => "Amazon.AWSToolkit.CodeCommit.Resources.EmbeddedImages.service-root-node.png";

        protected override void LoadChildren()
        {
            var items = new List<IViewModel>();
            
            // not a paginated api at present :-(
            var request = new ListRepositoriesRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

            var response = this.CodeCommitClient.ListRepositories(request);

            foreach (var r in response.Repositories)
            {
                var child = new CodeCommitRepositoryViewModel(this._metaNode.CodeCommitRepositoryViewMetaNode, this, r);
                items.Add(child);
            }

            BeginCopingChildren(items);
        }

        protected override void BuildClient(AWSCredentials credentials)
        {
            var config = new AmazonCodeCommitConfig { MaxErrorRetry = 6 };
            this.CurrentEndPoint.ApplyToClientConfig(config);
            CodeCommitClient = new AmazonCodeCommitClient(credentials, config);
        }

        public IAmazonCodeCommit CodeCommitClient { get; private set; }
    }
}