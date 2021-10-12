using System.Collections.Generic;
using System.Windows;
using Amazon.CodeArtifact;
using Amazon.CodeArtifact.Model;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;

using log4net;
using Amazon.Runtime;
using System;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.CodeArtifact.Nodes
{
    public class CodeArtifactRootViewModel : ServiceRootViewModel, ICodeArtifactRootViewModel
    {
        private readonly CodeArtifactRootViewMetaNode _metaNode;
        private readonly Lazy<IAmazonCodeArtifact> _codeArtifactClient;

        public CodeArtifactRootViewModel(AccountViewModel accountViewModel, ToolkitRegion region)
            : base(accountViewModel.MetaNode.FindChild<CodeArtifactRootViewMetaNode>(), accountViewModel, "Amazon CodeArtifact", region)
        {
            _metaNode = base.MetaNode as CodeArtifactRootViewMetaNode;
            _codeArtifactClient = new Lazy<IAmazonCodeArtifact>(CreateCodeArtifactClient);
        }

        public override string ToolTip => "Amazon CodeArtifact is a fully managed artifact repository service that makes it easy for organizations of any size to securely store, publish, and share software packages used in their software development process.";

        protected override string IconName => AwsImageResourcePath.CodeArtifact.Path;

        public IAmazonCodeArtifact CodeArtifactClient => this._codeArtifactClient.Value;

        protected override void LoadChildren()
        {
            try
            {
                List<IViewModel> items = new List<IViewModel>();
                var listDomainRequest = new ListDomainsRequest();
                do
                {
                    ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)listDomainRequest).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
                    var listDomainResponse = this.CodeArtifactClient.ListDomains(listDomainRequest);
                    foreach (var domain in listDomainResponse.Domains)
                    {
                        string domainName = domain.Name;
                        var listRepoInDomainRequest = new ListRepositoriesInDomainRequest() { Domain = domainName };
                        do
                        {
                            var listRepoInDomainResponse = this.CodeArtifactClient.ListRepositoriesInDomain(listRepoInDomainRequest);
                            var child = new DomainViewModel(this._metaNode.DomainViewMetaNode, this, domain, listRepoInDomainResponse.Repositories);
                            items.Add(child);
                            listRepoInDomainRequest.NextToken = listRepoInDomainResponse.NextToken;
                        } while (!string.IsNullOrEmpty(listRepoInDomainRequest.NextToken));
                    }
                    listDomainRequest.NextToken = listDomainResponse.NextToken;
                } while (!string.IsNullOrEmpty(listDomainRequest.NextToken));
                SetChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData("ARN", "arn:aws:CodeArtifact:::*");
        }

        private IAmazonCodeArtifact CreateCodeArtifactClient()
        {
            return AccountViewModel.CreateServiceClient<AmazonCodeArtifactClient>(Region);
        }
    }
}
