using Amazon.EC2;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public abstract class FeatureViewModel : AbstractViewModel
    {
        IAmazonEC2 _ec2Client;

        public FeatureViewModel(IMetaNode metaNode, EC2ServiceViewModel viewModel, string name)
            : base(metaNode, viewModel, name)
        {
            this._ec2Client = viewModel.EC2Client;
        }

        public IAmazonEC2 EC2Client => this._ec2Client;

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
                var region = RegionEndPointsManager.GetInstance().GetRegion(this.RegionSystemName);
                if (region == null)
                    return string.Empty;

                return region.DisplayName;
            }
        }
    }
}
