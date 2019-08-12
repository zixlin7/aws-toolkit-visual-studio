namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2KeyPairsViewModel : FeatureViewModel
    {
        public EC2KeyPairsViewModel(EC2KeyPairsViewMetaNode metaNode, EC2RootViewModel viewModel)
            : base(metaNode, viewModel, "Key Pairs")
        {
        }

        public override string ToolTip => "Manage EC2 key pairs and store private keys";

        protected override string IconName => "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.key-pairs.gif";
    }
}
