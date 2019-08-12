using System.Collections.Generic;
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
                    this.GetType().Assembly, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.CreateDBDubnetGroup.png"),
                null,
                new ActionHandlerWrapper("View", OnView, null, true,
                    this.GetType().Assembly, null)
            );

        public ActionHandlerWrapper.ActionHandler OnCreate
        {
            get;
            set;
        }
    }
}
