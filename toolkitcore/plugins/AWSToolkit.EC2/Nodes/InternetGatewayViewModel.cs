using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class InternetGatewayViewModel : FeatureViewModel
    {
        public InternetGatewayViewModel(InternetGatewayViewMetaNode metaNode, VPCRootViewModel viewModel)
            : base(metaNode, viewModel, "Internet Gateways")
        {
        }

        public override string ToolTip => "Create and associate internet gateways to vpcs";

        protected override string IconName => AwsImageResourcePath.VpcInternetGateway.Path;
    }
}
