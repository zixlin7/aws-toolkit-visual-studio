using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
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
                    typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.ElasticContainerServiceCluster.Path),
                null,
                new ActionHandlerWrapper("Delete",
                    OnDelete,
                    null,
                    false,
                    null,
                    "delete.png")
            );
    }
}
