using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.CodeArtifact.Interface.Nodes
{
    public interface IDomainViewModel : IViewModel
    {
        IAmazonCodeArtifact CodeArtifactClient { get; }
    }
}
