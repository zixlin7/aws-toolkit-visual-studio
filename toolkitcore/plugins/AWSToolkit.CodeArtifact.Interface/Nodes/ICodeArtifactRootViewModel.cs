using Amazon.CodeArtifact;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CodeArtifact.Nodes
{
    public interface ICodeArtifactRootViewModel : IServiceRootViewModel
    {
        IAmazonCodeArtifact CodeArtifactClient { get; }
    }
}
