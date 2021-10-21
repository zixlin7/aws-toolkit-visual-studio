using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2KeyPairsViewModel : FeatureViewModel
    {
        public EC2KeyPairsViewModel(EC2KeyPairsViewMetaNode metaNode, EC2RootViewModel viewModel)
            : base(metaNode, viewModel, "Key Pairs")
        {
        }

        public override string ToolTip => "Manage EC2 key pairs and store private keys";

        protected override string IconName => AwsImageResourcePath.Ec2KeyPairs.Path;
    }
}
