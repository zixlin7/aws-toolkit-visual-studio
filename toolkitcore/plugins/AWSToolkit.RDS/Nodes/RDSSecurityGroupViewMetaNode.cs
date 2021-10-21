using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSSecurityGroupViewMetaNode : RDSFeatureViewMetaNode
    {
        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View", OnView, null, true,
                    typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.RdsSecurityGroup.Path),
                null,
                new ActionHandlerWrapper("Delete Security Group", OnDelete, null, false,
                    null, "delete.png")
            );

        public ActionHandlerWrapper.ActionHandler OnDelete
        {
            get;
            set;
        }


    }
}
