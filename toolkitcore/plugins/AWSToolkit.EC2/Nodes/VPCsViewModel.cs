using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class VPCsViewModel : FeatureViewModel
    {
        public VPCsViewModel(VPCsViewMetaNode metaNode, VPCRootViewModel viewModel)
            : base(metaNode, viewModel, "VPCs")
        {
        }

        public override string ToolTip => "Create and manage vpcs.";

        protected override string IconName => AwsImageResourcePath.VpcVpcs.Path;
    }
}
