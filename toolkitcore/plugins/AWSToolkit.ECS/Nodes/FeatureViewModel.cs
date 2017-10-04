using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECR;
using Amazon.ECS;

namespace Amazon.AWSToolkit.ECS.Model
{
    public abstract class FeatureViewModel : AbstractViewModel
    {
        private readonly IAmazonECS _ecsClient;
        private readonly IAmazonECR _ecrClient;

        protected FeatureViewModel(IMetaNode metaNode, RootViewModel viewModel, string name)
            : base(metaNode, viewModel, name)
        {
            this._ecsClient = viewModel.ECSClient;
            this._ecrClient = viewModel.ECRClient;
        }

        public IAmazonECS ECSClient
        {
            get { return this._ecsClient; }
        }

        public IAmazonECR ECRClient
        {
            get { return this._ecrClient; }
        }

        public string RegionSystemName
        {
            get
            {
                IEndPointSupport support = this.Parent as IEndPointSupport;
                return support.CurrentEndPoint.RegionSystemName;
            }
        }

        public string RegionDisplayName
        {
            get
            {
                var region = RegionEndPointsManager.Instance.GetRegion(this.RegionSystemName);
                if (region == null)
                    return string.Empty;

                return region.DisplayName;
            }
        }
    }
}
