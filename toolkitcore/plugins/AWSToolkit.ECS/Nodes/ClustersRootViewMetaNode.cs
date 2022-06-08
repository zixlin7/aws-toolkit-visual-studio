using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ClustersRootViewMetaNode : AbstractMetaNode
    {

        public ClusterViewMetaNode ClusterViewMetaNode => this.FindChild<ClusterViewMetaNode>();

        public override bool SupportsRefresh => true;
    }
}
