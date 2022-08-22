using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;
using Amazon.Runtime;
using Amazon.AWSToolkit.CodeCommit.Interface.Nodes;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.CodeCommit.Nodes
{
    public class CodeCommitRootViewModel : ServiceRootViewModel, ICodeCommitRootViewModel
    {
        private readonly CodeCommitRootViewMetaNode _metaNode;
        private readonly Lazy<IAmazonCodeCommit> _codeCommitClient;

        public CodeCommitRootViewModel(AccountViewModel accountViewModel, ToolkitRegion region)
            : base(accountViewModel.MetaNode.FindChild<CodeCommitRootViewMetaNode>(), accountViewModel, "AWS CodeCommit", region)
        {
            _metaNode = MetaNode as CodeCommitRootViewMetaNode;
            _codeCommitClient = new Lazy<IAmazonCodeCommit>(CreateCodeCommitClient);
        }

        public override string ToolTip => "AWS CodeCommit";

        protected override string IconName => AwsImageResourcePath.CodeCommit.Path;

        protected override void LoadChildren()
        {
            var items = new List<IViewModel>();
            
            // not a paginated api at present :-(
            var request = new ListRepositoriesRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);

            var response = CodeCommitClient.ListRepositories(request);

            foreach (var r in response.Repositories)
            {
                var child = new CodeCommitRepositoryViewModel(_metaNode.CodeCommitRepositoryViewMetaNode, this, r);
                items.Add(child);
            }

            SetChildren(items);
        }

        public IAmazonCodeCommit CodeCommitClient => _codeCommitClient.Value;

        private IAmazonCodeCommit CreateCodeCommitClient()
        {
            var config = new AmazonCodeCommitConfig { MaxErrorRetry = 6 };
            return AccountViewModel.CreateServiceClient<AmazonCodeCommitClient>(Region, config);
        }
    }
}
