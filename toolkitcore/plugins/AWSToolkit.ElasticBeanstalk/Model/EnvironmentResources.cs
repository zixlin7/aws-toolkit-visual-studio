using System.Collections.Generic;

using Amazon.AutoScaling.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class EnvironmentResources
    {
        public List<InstanceWrapper> Instances { get; set; }
        public List<LoadBalancer> LoadBalancers { get; set; }
        public List<AutoScalingGroupWrapper> AutoScalingGroups { get; set; }
        public List<MetricAlarmWrapper> Alarms { get; set; }
    }
}
