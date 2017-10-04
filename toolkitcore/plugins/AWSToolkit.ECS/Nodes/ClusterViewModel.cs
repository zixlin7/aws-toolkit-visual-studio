using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECS;
using log4net;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ClusterViewModel : FeatureViewModel
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ClusterViewModel));

        readonly RootViewModel _rootViewModel;
        readonly IAmazonECS _ecsClient;
        readonly ClusterWrapper _cluster;

        public ClusterViewModel(ClusterViewMetaNode metaNode, RootViewModel viewModel, ClusterWrapper cluster)
            : base(metaNode, viewModel, cluster.Name)
        {
            this._rootViewModel = viewModel;
            this._cluster = cluster;
            this._ecsClient = viewModel.ECSClient;
        }

        public RootViewModel RootViewModel
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
