using Amazon.AWSToolkit.Navigator.Node;
using Amazon.CodeArtifact;
using Amazon.CodeArtifact.Interface.Nodes;
using Amazon.CodeArtifact.Model;
using log4net;
using System;
using System.Collections.Generic;

namespace Amazon.AWSToolkit.CodeArtifact.Nodes
{
    public class DomainViewModel : InstanceDataRootViewModel, IDomainViewModel
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(RepoViewModel));

        private static readonly string DomainIcon =
            "Amazon.AWSToolkit.CodeArtifact.Resources.EmbeddedImages.bucket.png";

        DomainViewMetaNode _metaNode;
        private CodeArtifactRootViewModel _serviceModel;
        DomainSummary _domain;
        string _iconName;
        private IAmazonCodeArtifact _rootCodeArtifactClient;

        public DomainViewModel(DomainViewMetaNode metaNode, CodeArtifactRootViewModel viewModel, DomainSummary domain)
            : base(metaNode, viewModel, domain.Name)
        {
            this._metaNode = metaNode;
            this._serviceModel = viewModel;
            this._domain = domain;
            this._iconName = DomainIcon;
            this._rootCodeArtifactClient = viewModel.CodeArtifactClient;
        }

        public DomainViewModel(DomainViewMetaNode metaNode, CodeArtifactRootViewModel viewModel, DomainSummary domain, IEnumerable<RepositorySummary> repositories)
            : this(metaNode, viewModel, domain)
        {
            AddRepositories(repositories);
        }

        private void AddRepositories(IEnumerable<RepositorySummary> repositories)
        {
            List<IViewModel> items = new List<IViewModel>();
            foreach (var repository in repositories)
            {
                items.Add(new RepoViewModel(this._metaNode.RepoViewMetaNode, this, repository));
            }

            SetChildren(items);
        }

        protected override void LoadChildren()
        {
            try
            {
                List<RepositorySummary> repositories = new List<RepositorySummary>();
                var request = new ListRepositoriesInDomainRequest() { Domain = this.Domain.Name };
                ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
                do
                {
                    var response = this.CodeArtifactClient.ListRepositoriesInDomain(request);
                    request.NextToken = response.NextToken;
                    response.Repositories.ForEach(repository => repositories.Add(repository));
                } while (!string.IsNullOrEmpty(request.NextToken)) ;
            AddRepositories(repositories);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }

        protected override string IconName
        {
            get
            {
                return _iconName;
            }
        }

        public IAmazonCodeArtifact CodeArtifactClient
        {
            get
            {
                return this._rootCodeArtifactClient;
            }
        }

        public DomainSummary Domain => this._domain;

    }
}
