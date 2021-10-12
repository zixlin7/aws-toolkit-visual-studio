using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class InternetGatewayViewMetaNode : FeatureViewMetaNode
    {

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View", OnView, null, true,
                    typeof(AwsImageResourcePath).Assembly, AwsImageResourcePath.VpcInternetGateway.Path)
            );
    }
}
