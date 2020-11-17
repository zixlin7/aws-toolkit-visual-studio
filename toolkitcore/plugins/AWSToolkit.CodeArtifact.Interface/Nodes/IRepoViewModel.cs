using Amazon.CodeArtifact;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CodeArtifact.Nodes
{
    public interface IRepoViewModel : IViewModel
    {
        IAmazonCodeArtifact CodeArtifactClient { get; }
    }
}
