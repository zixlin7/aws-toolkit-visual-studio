using Amazon.RDS;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.RDS.Nodes
{
    public class RDSFeatureViewModel : AbstractViewModel
    {
        readonly IAmazonRDS _rdsClient;

        public RDSFeatureViewModel(IMetaNode metaNode, RDSRootViewModel viewModel, string name)
            : base(metaNode, viewModel, name)
        {
            this._rdsClient = viewModel.RDSClient;
        }

        public IAmazonRDS RDSClient => this._rdsClient;

        public string RegionSystemName
        {
            get
            {
                var support = this.Parent as IEndPointSupport;
                return support.CurrentEndPoint.RegionSystemName;
            }
        }

        public string RegionDisplayName
        {
            get
            {
                var region = RegionEndPointsManager.GetInstance().GetRegion(this.RegionSystemName);
                if (region == null)
                    return string.Empty;

                return region.DisplayName;
            }
        }

    }
}
