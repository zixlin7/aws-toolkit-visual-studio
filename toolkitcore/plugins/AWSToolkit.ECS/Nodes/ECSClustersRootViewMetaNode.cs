using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ECSClustersRootViewMetaNode : ECSFeatureViewMetaNode
    {

        public ECSClusterViewMetaNode ECSClusterViewMetaNode
        {
            get { return this.FindChild<ECSClusterViewMetaNode>(); }
        }

        public override bool SupportsRefresh
        {
            get { return true; }
        }

        public ActionHandlerWrapper.ActionHandler OnLaunchCluster
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Launch Cluster...", 
                                             OnLaunchCluster, 
                                             null, 
                                             false,
                                             this.GetType().Assembly, 
                                             "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.launch_cluster.png"),
                    null,
                    new ActionHandlerWrapper("View", 
                                             OnView, 
                                             null, 
                                             true,
                                             this.GetType().Assembly, 
                                             null)
                );
            }
        }
    }
}
