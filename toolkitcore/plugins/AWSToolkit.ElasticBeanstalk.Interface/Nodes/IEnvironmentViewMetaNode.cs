using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public interface IEnvironmentViewMetaNode : IMetaNode
    {
        ActionHandlerWrapper.ActionHandler OnEnvironmentStatus { get; set; }
        ActionHandlerWrapper.ActionHandler OnRestartApp { get; set; }
        ActionHandlerWrapper.ActionHandler OnTerminateEnvironment { get; set; }
        ActionHandlerWrapper.ActionHandler OnRebuildingEnvironment { get; set; }
        ActionHandlerWrapper.ActionHandler OnCreateConfig { get; set; }
    }
}
