using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;


namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ECSClustersViewMetaNode : FeatureViewMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnLaunchCluster
        {
            get;
            set;
        }

        public void OnLaunchResponse(IViewModel focus, ActionResults results)
        {
            ECSRootViewModel rootModel = focus as ECSRootViewModel;
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Launch cluster...",
                                             OnLaunchCluster, 
                                             this.OnLaunchResponse, 
                                             false,
                                             this.GetType().Assembly, 
                                             "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.launch-cluster.png"),
                    null,
                    new ActionHandlerWrapper("View", 
                                             OnView, 
                                             null, 
                                             true,
                                             this.GetType().Assembly, 
                                             "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.clusters.png")
                    );
            }
        }
    }
}
