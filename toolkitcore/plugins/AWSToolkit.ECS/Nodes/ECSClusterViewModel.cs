using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECS;
using log4net;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ECSClusterViewModel : ECSFeatureViewModel
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ECSClusterViewModel));

        readonly ECSRootViewModel _rootViewModel;
        readonly IAmazonECS _ecsClient;
        readonly ClusterWrapper _cluster;

        public ECSClusterViewModel(ECSClusterViewMetaNode metaNode, ECSRootViewModel viewModel, ClusterWrapper cluster)
            : base(metaNode, viewModel, cluster.Name)
        {
            this._rootViewModel = viewModel;
            this._cluster = cluster;
            this._ecsClient = viewModel.ECSClient;
        }

        public ECSRootViewModel RootViewModel
        {
            get { return this._rootViewModel; }
        }

        public ClusterWrapper Cluster
        {
            get { return this._cluster; }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.clusters.png";
            }
        }
    }
}
