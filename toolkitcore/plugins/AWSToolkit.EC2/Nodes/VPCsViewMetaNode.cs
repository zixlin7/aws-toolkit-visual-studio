using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;


namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class VPCsViewMetaNode : FeatureViewMetaNode
    {

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View", OnView, null, true,
                    this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.vpc.png")
            );
    }
}
