using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CloudFront.Nodes
{
    public class CloudFrontStreamingDistributeViewMetaNode : BaseDistributeViewMetaNode
    {

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Properties", OnProperties, null, true, null, "properties.png"),
                    new ActionHandlerWrapper("Delete Distribution", OnDeleteDistribution, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false, null, "delete.png"));
            }
        }
    }
}
