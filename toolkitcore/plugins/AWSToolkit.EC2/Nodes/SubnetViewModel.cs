namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class SubnetViewModel : FeatureViewModel
    {
        public SubnetViewModel(SubnetViewMetaNode metaNode, VPCRootViewModel viewModel)
            : base(metaNode, viewModel, "Subnets")
        {
        }

        public override string ToolTip => "Create and manage subnets.";

        protected override string IconName => "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.subnet.png";
    }
}
