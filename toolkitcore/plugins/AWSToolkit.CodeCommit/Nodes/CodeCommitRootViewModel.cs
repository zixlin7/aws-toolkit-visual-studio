using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;
using Amazon.Runtime;
using Amazon.AWSToolkit.CodeCommit.Interface.Nodes;
using Amazon.AWSToolkit.CodeCommit.Model;
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

        public override string ToolTip
        {
            get
            {
                return "AWS CodeCommit gives you ... I dunno ... wings?";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.CodeCommit.Resources.EmbeddedImages.service-root-node.png";
            }
        }

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
            var config = new AmazonCodeCommitConfig { MaxErrorRetry = 6, ServiceURL = this.CurrentEndPoint.Url };
            if (this.CurrentEndPoint.Signer != null)
                config.SignatureVersion = this.CurrentEndPoint.Signer;
            if (this.CurrentEndPoint.AuthRegion != null)
                config.AuthenticationRegion = this.CurrentEndPoint.AuthRegion;
            CodeCommitClient = new AmazonCodeCommitClient(credentials, config);
        }

        public IAmazonCodeCommit CodeCommitClient { get; private set; }
    }
}