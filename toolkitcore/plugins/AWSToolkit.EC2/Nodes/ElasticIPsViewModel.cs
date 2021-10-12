using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class ElasticIPsViewModel : FeatureViewModel
    {
        public ElasticIPsViewModel(ElasticIPsViewMetaNode metaNode, EC2ServiceViewModel viewModel)
            : base(metaNode, viewModel, "Elastic IPs")
        {
        }

        public override string ToolTip => "Create and associate elastic ips to EC2 instances";

        protected override string IconName => AwsImageResourcePath.Ec2ElasticIpAddress.Path;
    }
}
