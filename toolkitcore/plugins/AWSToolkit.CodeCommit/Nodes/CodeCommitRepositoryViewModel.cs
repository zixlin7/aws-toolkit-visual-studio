using System.Windows;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;

using Amazon.AWSToolkit.CodeCommit.Interface.Nodes;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.CodeCommit.Nodes
{
    public class CodeCommitRepositoryViewModel : AbstractViewModel, ICodeCommitRepositoryViewModel
    {
        CodeCommitRepositoryViewMetaNode _metaNode;

        public CodeCommitRepositoryViewModel(CodeCommitRepositoryViewMetaNode metaNode, CodeCommitRootViewModel viewModel, RepositoryNameIdPair repositoryNameAndID)
            : base(metaNode, viewModel, repositoryNameAndID.RepositoryName)
        {
            this._metaNode = metaNode;
            this.CodeCommitRootViewModel = viewModel;
            this.RepositoryNameAndID = repositoryNameAndID;
            this.CodeCommitClient = this.CodeCommitRootViewModel.CodeCommitClient;
        }

        protected override string IconName => AwsImageResourcePath.CodeCommitRepository.Path;

        public IAmazonCodeCommit CodeCommitClient { get; }

        public RepositoryNameIdPair RepositoryNameAndID { get; }

        public CodeCommitRootViewModel CodeCommitRootViewModel { get; }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
        }
    }
}
