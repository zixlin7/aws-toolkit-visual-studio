using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSSecurityGroupRootViewMetaNode : RDSFeatureViewMetaNode
    {

        public RDSSecurityGroupViewMetaNode RDSSecurityGroupViewMetaNode
        {
            get { return this.FindChild<RDSSecurityGroupViewMetaNode>(); }
        }

        public override bool SupportsRefresh
        {
            get { return true; }
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Create...", OnCreate, null, false,
                        this.GetType().Assembly, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.CreateDBDubnetGroup.png"),
                    null,
                    new ActionHandlerWrapper("View", OnView, null, true,
                        this.GetType().Assembly, null)
                    );
            }
        }

        public ActionHandlerWrapper.ActionHandler OnCreate
        {
            get;
            set;
        }
    }
}
