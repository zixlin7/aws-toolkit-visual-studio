using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;


namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class ClusterViewMetaNode : FeatureViewMetaNode
    {
        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
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
