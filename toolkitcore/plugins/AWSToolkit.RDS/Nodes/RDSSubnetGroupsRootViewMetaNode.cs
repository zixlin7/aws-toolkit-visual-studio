using System.Collections.Generic;
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
                    this.GetType().Assembly, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.CreateDBSubnetGroup.png"),
                null,
                new ActionHandlerWrapper("View", OnView, null, true,
                    this.GetType().Assembly, null)
            );
    }
}
