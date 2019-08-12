using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.CloudFront.Nodes
{
    public class CloudFrontDistributeViewMetaNode : BaseDistributeViewMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnViewInvalidationRequests
        {
            get;
            set;
        }


        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View Invalidation Requests", OnViewInvalidationRequests, null, false, null, null),
                new ActionHandlerWrapper("Properties", OnProperties, null, true, null, "properties.png"),
                new ActionHandlerWrapper("Delete Distribution", OnDeleteDistribution, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false, null, "delete.png"));
    }
}
