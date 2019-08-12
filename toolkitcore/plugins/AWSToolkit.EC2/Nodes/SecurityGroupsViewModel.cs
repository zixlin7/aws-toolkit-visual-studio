namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class SecurityGroupsViewModel : FeatureViewModel
    {
        public SecurityGroupsViewModel(SecurityGroupsViewMetaNode metaNode, EC2ServiceViewModel viewModel)
            : base(metaNode, viewModel, "Security Groups")
        {
        }

        public override string ToolTip => "Create and manage EC2 and VPC security groups.";

        protected override string IconName => "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.security-groups.png";
    }
}
