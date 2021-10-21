using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2AMIsViewModel : FeatureViewModel
    {
        public EC2AMIsViewModel(EC2AMIsViewMetaNode metaNode, EC2RootViewModel viewModel)
            : base(metaNode, viewModel, "AMIs")
        {
        }

        public override string ToolTip => "View and manage Amazon Machine Images and create new EC2 instances from Amazon Machine Images";

        protected override string IconName => AwsImageResourcePath.Ec2Ami.Path;
    }
}
