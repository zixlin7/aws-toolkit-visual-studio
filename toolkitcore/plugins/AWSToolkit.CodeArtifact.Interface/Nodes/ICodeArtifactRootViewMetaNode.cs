using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CodeArtifact.Nodes
{
    public interface ICodeArtifactRootViewMetaNode : IServiceRootViewMetaNode
    {
        ActionHandlerWrapper.ActionHandler SelectProfile { get; set; }
    }
}
