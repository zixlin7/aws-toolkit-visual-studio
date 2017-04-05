using System;
using System.Windows;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;

using Amazon.AWSToolkit.CodeCommit.Interface.Nodes;
using Amazon.AWSToolkit.CodeCommit.Model;
using LibGit2Sharp;

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

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.CodeCommit.Resources.EmbeddedImages.repository-node.png";
            }
        }

        public IAmazonCodeCommit CodeCommitClient { get; private set; }

        public RepositoryNameIdPair RepositoryNameAndID { get; private set; }

        public CodeCommitRootViewModel CodeCommitRootViewModel { get; private set; }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
        }

    }
}