using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ClustersRootViewMetaNode : AbstractMetaNode
    {

        public ClusterViewMetaNode ClusterViewMetaNode => this.FindChild<ClusterViewMetaNode>();

        public override bool SupportsRefresh => true;

        public ActionHandlerWrapper.ActionHandler OnLaunchCluster
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Launch Cluster...", 
                    OnLaunchCluster, 
                    null, 
                    false,
                    this.GetType().Assembly, 
                    "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.launch_cluster.png")
            );
    }
}
