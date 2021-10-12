using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSSecurityGroupRootViewMetaNode : RDSFeatureViewMetaNode
    {

        public RDSSecurityGroupViewMetaNode RDSSecurityGroupViewMetaNode => this.FindChild<RDSSecurityGroupViewMetaNode>();

        public override bool SupportsRefresh => true;

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Create...", OnCreate, null, false,
                    typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.RdsSecurityGroup.Path),
                null,
                new ActionHandlerWrapper("View", OnView, null, true,
                    typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.RdsSecurityGroup.Path)
            );

        public ActionHandlerWrapper.ActionHandler OnCreate
        {
            get;
            set;
        }
    }
}
