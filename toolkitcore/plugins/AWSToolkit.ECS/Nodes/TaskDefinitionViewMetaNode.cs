using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class TaskDefinitionViewMetaNode : FeatureViewMetaNode
    {
        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View",
                    OnView,
                    null,
                    true,
                    this.GetType().Assembly,
                    "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.taskdef.png")
            );
    }
}
