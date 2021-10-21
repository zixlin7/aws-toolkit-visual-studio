using Amazon.CodeArtifact;
using Amazon.AWSToolkit.Navigator.Node;
using log4net;
using Amazon.CodeArtifact.Model;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.CodeArtifact.Nodes
{
    public class RepoViewModel : AbstractViewModel, IRepoViewModel
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(RepoViewModel));

        private static readonly string RepoIcon =
             AwsImageResourcePath.CodeArtifactRepository.Path;

        RepoViewMetaNode _metaNode;
        DomainViewModel _serviceModel;
        IAmazonCodeArtifact _rootCodeArtifactClient;

        string _iconName;

        public RepoViewModel(RepoViewMetaNode metaNode, DomainViewModel viewModel, RepositorySummary repository)
            : base(metaNode, viewModel, repository.Name)
        {
            this._metaNode = metaNode;
            this._serviceModel = viewModel;
            this._rootCodeArtifactClient = viewModel.CodeArtifactClient;
            this._iconName = RepoIcon;
        }

        protected override string IconName
        {
            get
            {
                return _iconName;
            }
        }

        public IAmazonCodeArtifact CodeArtifactClient
        {
            get
            {
                return this._rootCodeArtifactClient;
            }
        }
    }
}
