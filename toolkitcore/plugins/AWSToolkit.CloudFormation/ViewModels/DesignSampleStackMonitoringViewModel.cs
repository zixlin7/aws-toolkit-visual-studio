namespace Amazon.AWSToolkit.CloudFormation.ViewModels
{
    public class DesignSampleStackMonitoringViewModel : StackMonitoringViewModel
    {
        public DesignSampleStackMonitoringViewModel()
        {
            Charts.Add(new LambdaResourceChartsViewModel("Sample Function", null));
            Charts.Add(new ApplicationLoadBalancerResourceChartsViewModel("Sample Load Balancer", null));
            Charts.Add(new Ec2InstanceResourceChartsViewModel("Sample EC2 Instance", null));
        }
    }
}
