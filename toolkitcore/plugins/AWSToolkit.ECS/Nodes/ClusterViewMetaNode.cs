using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;


namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ClusterViewMetaNode : FeatureViewMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnDelete
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View", 
                    OnView, 
                    null, 
                    true,
                    this.GetType().Assembly, 
                    "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.clusters.png"),
                null,
                new ActionHandlerWrapper("Delete",
                    OnDelete,
                    null,
                    false,
                    this.GetType().Assembly,
                    "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.delete-cluster.png")
            );
    }
}
