using Amazon.AWSToolkit.Navigator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CloudFormation.Nodes
{
    public interface ICloudFormationStackViewMetaNode
    {
        ActionHandlerWrapper.ActionHandler OnDelete { get; }
        ActionHandlerWrapper.ActionHandler OnOpen { get; }
        ActionHandlerWrapper.ActionHandler OnCreateConfig { get; }
    }
}
