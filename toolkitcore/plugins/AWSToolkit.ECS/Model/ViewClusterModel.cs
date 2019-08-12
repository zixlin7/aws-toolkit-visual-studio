using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;
using Amazon.ElasticLoadBalancingV2.Model;

using Amazon.ECS;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class ViewClusterModel : BaseModel
    {
        public ClusterWrapper Cluster { get; internal set; }

        public ObservableCollection<ServiceWrapper> Services { get; } = new ObservableCollection<ServiceWrapper>();

        public ObservableCollection<TaskWrapper> Tasks { get; } = new ObservableCollection<TaskWrapper>();
        public ObservableCollection<ScheduledTaskWrapper> ScheduledTasks { get; } = new ObservableCollection<ScheduledTaskWrapper>();

        public DesiredStatus TaskTabDesiredStatus { get; set; } = DesiredStatus.RUNNING;

        internal LoadBalancerState LBState { get;set; }

        internal class LoadBalancerState
        {
            public Dictionary<string, TargetGroup> TargetGroups = new Dictionary<string, TargetGroup>();
            public Dictionary<string, Amazon.ElasticLoadBalancingV2.Model.LoadBalancer> LoadBalancers = new Dictionary<string, Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>();
            public Dictionary<string, List<Listener>> ListenersByLoadBalancerArn = new Dictionary<string, List<Listener>>();
            public Dictionary<string, List<Rule>> RulesByListenerArn = new Dictionary<string, List<Rule>>();
        }
    }
}
