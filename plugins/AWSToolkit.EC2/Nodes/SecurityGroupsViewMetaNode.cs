using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class SecurityGroupsViewMetaNode : FeatureViewMetaNode
    {
        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("View", OnView, null, true,
                        this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.security-groups.png")
                    );
            }
        }
    }
}
