using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2InstancesViewModel : FeatureViewModel, IEC2InstancesViewModel
    {
        public EC2InstancesViewModel(EC2InstancesViewMetaNode metaNode, EC2RootViewModel viewModel)
            : base(metaNode, viewModel, "Instances") { }

        protected override string IconName => AwsImageResourcePath.Ec2Instances.Path;

        public override string ToolTip => "Manage EC2 instances and launch new EC2 instances";
    }
}
