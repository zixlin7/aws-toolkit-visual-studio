using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECR;
using Amazon.ECR.Model;

using log4net;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class RepositoriesRootViewModel : InstanceDataRootViewModel
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(RepositoriesRootViewModel));

        readonly RootViewModel _rootViewModel;
        readonly IAmazonECR _ecrClient;

        public RepositoriesRootViewModel(RepositoriesRootViewMetaNode metaNode, RootViewModel viewModel)
            : base(metaNode, viewModel, "Repositories")
        {
            this._rootViewModel = viewModel;
            this._ecrClient = viewModel.ECRClient;
        }

        public IAmazonECR ECRClient => this._ecrClient;

        public RootViewModel EcsRootViewModel => _rootViewModel;

        protected override string IconName => AwsImageResourcePath.ElasticContainerRegistry.Path;
        protected override void LoadChildren()
        {
            var items = new List<IViewModel>();

            var request = new DescribeRepositoriesRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(AWSToolkit.Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
            do
            {
                var response = this.ECRClient.DescribeRepositories(request);
                items.AddRange(response.Repositories.Select(repo =>
                    new RepositoryViewModel(this.MetaNode.FindChild<RepositoryViewMetaNode>(),
                        this,
                        new RepositoryWrapper(repo))).Cast<IViewModel>().ToList());

                request.NextToken = response.NextToken;
            } while (!string.IsNullOrEmpty(request.NextToken));

            SetChildren(items);
        }

        public void RemoveRepositoryInstance(string repositoryName)
        {
            base.RemoveChild(repositoryName);
        }

        public void AddRepository(RepositoryWrapper instance)
        {
            var child = new RepositoryViewModel(this.MetaNode.FindChild<RepositoryViewMetaNode>(), this, instance);
            base.AddChild(child);
        }
    }
}
