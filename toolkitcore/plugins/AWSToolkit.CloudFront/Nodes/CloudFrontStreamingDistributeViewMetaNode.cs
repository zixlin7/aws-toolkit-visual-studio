using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.CloudFront.Nodes
{
    public class CloudFrontStreamingDistributeViewMetaNode : BaseDistributeViewMetaNode
    {

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Properties", OnProperties, null, true, null, "properties.png"),
                new ActionHandlerWrapper("Delete Distribution", OnDeleteDistribution, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false, null, "delete.png"));
    }
}
