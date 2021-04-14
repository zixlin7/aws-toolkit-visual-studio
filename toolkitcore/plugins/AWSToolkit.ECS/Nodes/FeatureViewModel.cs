using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
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

        public IAmazonECS ECSClient => this._ecsClient;

        public IAmazonECR ECRClient => this._ecrClient;

        public ToolkitRegion Region
        {
            get
            {
                IEndPointSupport support = this.Parent as IEndPointSupport;
                return support.Region;
            }
        }
    }
}
