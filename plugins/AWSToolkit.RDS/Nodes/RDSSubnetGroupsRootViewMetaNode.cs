using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSSubnetGroupsRootViewMetaNode : RDSFeatureViewMetaNode
    {

        public RDSSubnetGroupViewMetaNode RDSSubnetGroupsViewMetaNode
        {
            get { return this.FindChild<RDSSubnetGroupViewMetaNode>(); }
        }

        public override bool SupportsRefresh
        {
            get { return true; }
        }

        public ActionHandlerWrapper.ActionHandler OnCreate
        {
            get;
            set;
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Create...", OnCreate, null, false,
                        this.GetType().Assembly, "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.CreateDBSubnetGroup.png"),
                    null,
                    new ActionHandlerWrapper("View", OnView, null, true,
                        this.GetType().Assembly, null)
                    );
            }
        }
    }
}
