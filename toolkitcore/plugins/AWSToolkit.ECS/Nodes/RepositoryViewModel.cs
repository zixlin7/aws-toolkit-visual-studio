using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public RepositoriesRootViewModel RootViewModel
        {
            get { return this._rootViewModel; }
        }

        public RepositoryWrapper Repository
        {
            get { return this._repository; }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.repository.png";
            }
        }

        public string RepositoryName
        {
            get { return _repository.Name; }
        }
    }
}
