using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class FeatureViewMetaNode : AbstractMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnView
        {
            get;
            set;
        }
    }
}
