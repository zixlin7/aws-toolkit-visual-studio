using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2KeyPairsViewMetaNode : FeatureViewMetaNode
    {
        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View", OnView, null, true,
                    this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.key-pairs.gif")
            );
    }
}
