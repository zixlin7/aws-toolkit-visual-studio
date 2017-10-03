using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator.Node;

using Amazon.ECS;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public interface IECSRootViewModel : IServiceRootViewModel
    {
        IAmazonECS ECSClient { get; }
    }
}
