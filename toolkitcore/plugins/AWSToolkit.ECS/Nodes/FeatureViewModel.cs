using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.ECS;

namespace Amazon.AWSToolkit.ECS.Model
{
    public abstract class FeatureViewModel : AbstractViewModel
    {
        readonly IAmazonECS _ecsClient;

        public FeatureViewModel(IMetaNode metaNode, ECSServiceViewModel viewModel, string name)
            : base(metaNode, viewModel, name)
        {
            this._ecsClient = viewModel.ECSClient;
        }

        public IAmazonECS ECSClient
        {
            get { return this._ecsClient; }
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
