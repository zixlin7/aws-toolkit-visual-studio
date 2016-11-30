using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.S3.Nodes
{
    public interface IS3RootViewMetaNode : IServiceRootViewMetaNode
    {
        ActionHandlerWrapper.ActionHandler OnCreate { get; set; }

        void OnCreateResponse(IViewModel focus, ActionResults results);
    }
}
