using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.ECS;
using Amazon.ECR;
using Amazon.Runtime;
using log4net;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class RootViewModel : ServiceRootViewModel, IECSRootViewModel
    {
        private readonly RootViewMetaNode _metaNode;
        private readonly Lazy<IAmazonECS> _ecsClient;
        private readonly Lazy<IAmazonECR> _ecrClient;

        public RootViewModel(AccountViewModel accountViewModel, ToolkitRegion region)
            : base(accountViewModel.MetaNode.FindChild<RootViewMetaNode>(), accountViewModel, "Amazon Elastic Container Service", region)
        {
            _metaNode = base.MetaNode as RootViewMetaNode;
            _ecsClient = new Lazy<IAmazonECS>(CreateEcsClient);
            _ecrClient = new Lazy<IAmazonECR>(CreateEcrClient);
        }

        public IAmazonECS ECSClient => this._ecsClient.Value;

        public IAmazonECR ECRClient => this._ecrClient.Value;

        public override string ToolTip =>
            "Amazon Elastic Container Service (Amazon ECS) is a highly scalable, fast, container management service that makes it easy to run, stop, "
            + "and manage Docker containers. Images may be managed using Amazon Elastic Container Registry (Amazon ECR), "
            + "a managed AWS Docker registry service";

        protected override string IconName => "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.service-root-icon.png";


        protected override void LoadChildren()
        {
            try
            {
                List<IViewModel> items = new List<IViewModel>
                {
                    new ClustersRootViewModel(this.MetaNode.FindChild<ClustersRootViewMetaNode>(), this),
                    //new TaskDefinitionsRootViewModel(this.MetaNode.FindChild<TaskDefinitionsRootViewMetaNode>(), this),
                    new RepositoriesRootViewModel(this.MetaNode.FindChild<RepositoriesRootViewMetaNode>(), this),
                };
                SetChildren(items);
            }
            catch (Exception e)
            {
                AddErrorChild(e);
            }
        }

        private IAmazonECS CreateEcsClient()
        {
            return AccountViewModel.CreateServiceClient<AmazonECSClient>(Region);
        }

        private IAmazonECR CreateEcrClient()
        {
            return AccountViewModel.CreateServiceClient<AmazonECRClient>(Region);
        }
    }
}
