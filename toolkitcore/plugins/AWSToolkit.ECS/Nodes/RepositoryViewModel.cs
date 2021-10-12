using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.ECR;
using log4net;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class RepositoryViewModel : FeatureViewModel
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(RepositoryViewModel));

        readonly RepositoriesRootViewModel _rootViewModel;
        readonly IAmazonECR _ecrClient;
        readonly RepositoryWrapper _repository;

        public RepositoryViewModel(RepositoryViewMetaNode metaNode, RepositoriesRootViewModel rootViewModel, RepositoryWrapper repository)
            : base(metaNode, rootViewModel.FindAncestor<RootViewModel>(), repository.Name)
        {
            this._rootViewModel = rootViewModel;
            this._repository = repository;
            this._ecrClient = rootViewModel.ECRClient;
        }

        public RepositoriesRootViewModel RootViewModel => this._rootViewModel;

        public RepositoryWrapper Repository => this._repository;

        protected override string IconName => AwsImageResourcePath.EcrRepository.Path;

        public string RepositoryName => _repository.Name;
    }
}
