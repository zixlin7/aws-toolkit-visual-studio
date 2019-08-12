using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSInstanceRootViewMetaNode : RDSFeatureViewMetaNode
    {

        public RDSInstanceViewMetaNode RDSInstanceViewMetaNode => this.FindChild<RDSInstanceViewMetaNode>();

        public override bool SupportsRefresh => true;

        public ActionHandlerWrapper.ActionHandler OnLaunchDBInstance
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Launch Instance...", OnLaunchDBInstance, null, false,
                    this.GetType().Assembly, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.DBInstance_Launch.png"),
                null,
                new ActionHandlerWrapper("View", OnView, null, true,
                    this.GetType().Assembly, null)
            );
    }
}
