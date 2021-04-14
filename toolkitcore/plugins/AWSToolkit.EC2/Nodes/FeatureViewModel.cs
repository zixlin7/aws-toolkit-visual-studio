using Amazon.EC2;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;

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
