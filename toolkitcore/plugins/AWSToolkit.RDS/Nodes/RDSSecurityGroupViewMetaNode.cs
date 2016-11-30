using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSSecurityGroupViewMetaNode : RDSFeatureViewMetaNode
    {
        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("View", OnView, null, true,
                        this.GetType().Assembly, null),
                    null,
                    new ActionHandlerWrapper("Delete Security Group", OnDelete, null, false,
                        null, "delete.png")
                    );
            }
        }

        public ActionHandlerWrapper.ActionHandler OnDelete
        {
            get;
            set;
        }


    }
}
