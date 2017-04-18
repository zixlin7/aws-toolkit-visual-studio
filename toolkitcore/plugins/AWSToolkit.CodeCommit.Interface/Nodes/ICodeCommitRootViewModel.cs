using Amazon.CodeCommit;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CodeCommit.Interface.Nodes
{
    public interface ICodeCommitRootViewModel : IServiceRootViewModel
    {
        IAmazonCodeCommit CodeCommitClient { get; }

    }
}
