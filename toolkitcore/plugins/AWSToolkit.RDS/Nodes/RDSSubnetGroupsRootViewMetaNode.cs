using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSSubnetGroupsRootViewMetaNode : RDSFeatureViewMetaNode
    {

        public RDSSubnetGroupViewMetaNode RDSSubnetGroupsViewMetaNode => this.FindChild<RDSSubnetGroupViewMetaNode>();

        public override bool SupportsRefresh => true;

        public ActionHandlerWrapper.ActionHandler OnCreate
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Create...", OnCreate, null, false,
                    typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.RdsSubnetGroups.Path),
                null,
                new ActionHandlerWrapper("View", OnView, null, true,
                    typeof(AwsImageResourcePath).Assembly,
                    AwsImageResourcePath.RdsSubnetGroups.Path)
            );
    }
}
