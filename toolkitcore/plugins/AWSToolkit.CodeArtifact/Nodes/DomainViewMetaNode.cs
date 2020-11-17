using Amazon.AWSToolkit.Navigator.Node;
using Amazon.CodeArtifact.Interface.Nodes;

namespace Amazon.AWSToolkit.CodeArtifact.Nodes
{
    public class DomainViewMetaNode : AbstractMetaNode, IDomainViewMetaNode
    {
        public RepoViewMetaNode RepoViewMetaNode => this.FindChild<RepoViewMetaNode>();
    }
}
