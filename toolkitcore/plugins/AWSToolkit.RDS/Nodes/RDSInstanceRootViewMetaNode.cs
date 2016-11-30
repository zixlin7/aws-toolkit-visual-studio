using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSInstanceRootViewMetaNode : RDSFeatureViewMetaNode
    {

        public RDSInstanceViewMetaNode RDSInstanceViewMetaNode
        {
            get { return this.FindChild<RDSInstanceViewMetaNode>(); }
        }

        public override bool SupportsRefresh
        {
            get { return true; }
        }

        public ActionHandlerWrapper.ActionHandler OnLaunchDBInstance
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Launch Instance...", OnLaunchDBInstance, null, false,
                        this.GetType().Assembly, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.DBInstance_Launch.png"),
                    null,
                    new ActionHandlerWrapper("View", OnView, null, true,
                        this.GetType().Assembly, null)
                    );
            }
        }
    }
}
