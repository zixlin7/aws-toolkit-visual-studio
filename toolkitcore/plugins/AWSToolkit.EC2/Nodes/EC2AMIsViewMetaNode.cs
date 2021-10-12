using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2AMIsViewMetaNode : FeatureViewMetaNode
    {

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View", OnView, null, true,
                    typeof(AwsImageResourcePath).Assembly, AwsImageResourcePath.Ec2Ami.Path)
            );
    }
}
