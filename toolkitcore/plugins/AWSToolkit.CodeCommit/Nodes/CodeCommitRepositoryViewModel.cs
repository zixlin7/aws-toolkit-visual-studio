using Amazon.AWSToolkit.CodeCommit.Interface.Nodes;
using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;

namespace Amazon.AWSToolkit.CodeCommit.Nodes
{
    public class CodeCommitRepositoryViewModel : AbstractViewModel, ICodeCommitRepositoryViewModel
    {
        public CodeCommitRepositoryViewModel(CodeCommitRepositoryViewMetaNode metaNode, CodeCommitRootViewModel viewModel, RepositoryNameIdPair repositoryNameAndID)
            : base(metaNode, viewModel, repositoryNameAndID.RepositoryName)
        {
            CodeCommitRootViewModel = viewModel;
            RepositoryNameAndID = repositoryNameAndID;
            CodeCommitClient = CodeCommitRootViewModel.CodeCommitClient;
        }

        protected override string IconName => AwsImageResourcePath.CodeCommitRepository.Path;

        public IAmazonCodeCommit CodeCommitClient { get; }

        public RepositoryNameIdPair RepositoryNameAndID { get; }

        public CodeCommitRootViewModel CodeCommitRootViewModel { get; }
    }
}
