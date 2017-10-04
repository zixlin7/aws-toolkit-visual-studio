using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.ECS;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECS.Model;
using log4net;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ECSClustersRootViewModel : InstanceDataRootViewModel
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ECSClustersRootViewModel));

        readonly ECSRootViewModel _rootViewModel;
        readonly IAmazonECS _ecsClient;

        public ECSClustersRootViewModel(ECSClustersRootViewMetaNode metaNode, ECSRootViewModel viewModel)
            : base(metaNode, viewModel, "Clusters")
        {
            this._rootViewModel = viewModel;
            this._ecsClient = viewModel.ECSClient;
        }

        public IAmazonECS ECSClient
        {
            get { return this._ecsClient; }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.clusters.png";
            }
        }

        protected override void LoadChildren()
        {
            var items = new List<IViewModel>();

            // The list api provides only the arn and we must do a describe (which can take
            // batches of 100 arns) to get the name. So, we parse name from the arn to avoid
            // the describe call, which we'll make if the user opens a specific cluster view
            var listRequest = new ListClustersRequest();
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)listRequest).AddBeforeRequestHandler(AWSToolkit.Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
            do
            {
                var listResponse = this.ECSClient.ListClusters(listRequest);
                items.AddRange(listResponse.ClusterArns.Select(arn =>
                    new ECSClusterViewModel(this.MetaNode.FindChild<ECSClusterViewMetaNode>(),
                                            this._rootViewModel,
                                            new ClusterWrapper(arn))).Cast<IViewModel>().ToList());

                listRequest.NextToken = listResponse.NextToken;
            } while (!string.IsNullOrEmpty(listRequest.NextToken));

            BeginCopingChildren(items);
        }

        public void RemoveClusterInstance(string clusterArn)
        {
            base.RemoveChild(clusterArn);
        }

        public void AddCluster(ClusterWrapper instance)
        {
            var child = new ECSClusterViewModel(this.MetaNode.FindChild<ECSClusterViewMetaNode>(), this._rootViewModel, instance);
            base.AddChild(child);
        }
    }
}
