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

        readonly RootViewModel _rootViewModel;
        readonly IAmazonECR _ecrClient;
        readonly RepositoryWrapper _repository;

        public RepositoryViewModel(RepositoryViewMetaNode metaNode, RootViewModel viewModel, RepositoryWrapper repository)
            : base(metaNode, viewModel, repository.Name)
        {
            this._rootViewModel = viewModel;
            this._repository = repository;
            this._ecrClient = viewModel.ECRClient;
        }

        public RootViewModel RootViewModel
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
