using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SNS.Nodes
{
    public interface ISNSTopicViewMetaNode : IMetaNode
    {
        ActionHandlerWrapper.ActionHandler OnViewTopic { get; set; }
        ActionHandlerWrapper.ActionHandler OnEditPolicy { get; set; }
        ActionHandlerWrapper.ActionHandler OnDelete { get; set; }

    }
}
