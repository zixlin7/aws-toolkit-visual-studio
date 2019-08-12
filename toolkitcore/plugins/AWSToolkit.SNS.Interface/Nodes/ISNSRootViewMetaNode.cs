using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SNS.Nodes
{
    public interface ISNSRootViewMetaNode : IServiceRootViewMetaNode
    {
        ActionHandlerWrapper.ActionHandler OnCreateTopic { get; set; }
        ActionHandlerWrapper.ActionHandler OnViewSubscriptions { get; set; }

    }
}
