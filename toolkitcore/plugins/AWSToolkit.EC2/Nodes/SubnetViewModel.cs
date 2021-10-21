using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class SubnetViewModel : FeatureViewModel
    {
        public SubnetViewModel(SubnetViewMetaNode metaNode, VPCRootViewModel viewModel)
            : base(metaNode, viewModel, "Subnets")
        {
        }

        public override string ToolTip => "Create and manage subnets.";

        protected override string IconName => AwsImageResourcePath.RdsSubnetGroups.Path;
    }
}
