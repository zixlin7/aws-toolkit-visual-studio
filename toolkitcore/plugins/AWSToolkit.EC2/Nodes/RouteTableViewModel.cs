namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class RouteTableViewModel : FeatureViewModel
    {
        public RouteTableViewModel(RouteTableViewMetaNode metaNode, VPCRootViewModel viewModel)
            : base(metaNode, viewModel, "Route Tables")
        {
        }

        public override string ToolTip => "Create and associate route tables with subnets.";

        protected override string IconName => "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.route-table.png";
    }
}
