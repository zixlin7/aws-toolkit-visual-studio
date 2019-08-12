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
