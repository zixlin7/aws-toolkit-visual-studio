using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECS;
using log4net;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ClusterViewModel : FeatureViewModel
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ClusterViewModel));

        readonly ClustersRootViewModel _rootViewModel;
        readonly IAmazonECS _ecsClient;
        readonly ClusterWrapper _cluster;

        public ClusterViewModel(ClusterViewMetaNode metaNode, ClustersRootViewModel rootViewModel, ClusterWrapper cluster)
            : base(metaNode, rootViewModel.FindAncestor<RootViewModel>(), cluster.Name)
        {
            this._rootViewModel = rootViewModel;
            this._cluster = cluster;
            this._ecsClient = rootViewModel.ECSClient;
        }

        public ClustersRootViewModel RootViewModel
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
