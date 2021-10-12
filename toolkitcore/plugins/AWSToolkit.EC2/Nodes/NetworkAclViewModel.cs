using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class NetworkAclViewModel : FeatureViewModel
    {
        public NetworkAclViewModel(NetworkAclViewMetaNode metaNode, VPCRootViewModel viewModel)
            : base(metaNode, viewModel, "Network ACLs")
        {
        }

        public override string ToolTip => "Create and associate network acls with subnets.";

        protected override string IconName => AwsImageResourcePath.VpcNetworkAccessControlList.Path;
    }
}
