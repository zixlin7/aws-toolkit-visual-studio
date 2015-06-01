using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSSecuirtyGroupViewMetaNode : RDSFeatureViewMetaNode
    {
        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("View", OnView, null, true,
                        this.GetType().Assembly, null)
                    );
            }
        }
    }
}
