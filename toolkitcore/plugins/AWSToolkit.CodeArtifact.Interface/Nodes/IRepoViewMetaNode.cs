using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CodeArtifact.Nodes
{
    public interface IRepoViewMetaNode : IMetaNode
    {
        ActionHandlerWrapper.ActionHandler GetRepoEndpoint { get; set; }
    }
}
