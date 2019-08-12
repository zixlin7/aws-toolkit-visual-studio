namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class VPCsViewModel : FeatureViewModel
    {
        public VPCsViewModel(VPCsViewMetaNode metaNode, VPCRootViewModel viewModel)
            : base(metaNode, viewModel, "VPCs")
        {
        }

        public override string ToolTip => "Create and manage vpcs.";

        protected override string IconName => "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.vpc.png";
    }
}
