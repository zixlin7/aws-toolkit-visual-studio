using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.ECS.Model;
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

        public ClustersRootViewModel RootViewModel => this._rootViewModel;

        public ClusterWrapper Cluster => this._cluster;

        protected override string IconName => AwsImageResourcePath.ElasticContainerServiceCluster.Path;
    }
}
