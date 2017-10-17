using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.CommonUI;
using Amazon.ElasticLoadBalancingV2.Model;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class ViewClusterModel : BaseModel
    {
        public ClusterWrapper Cluster { get; internal set; }

        public ObservableCollection<ServiceWrapper> Services { get; } = new ObservableCollection<ServiceWrapper>();

        internal LoadBalancerState LBState { get;set; }

        internal class LoadBalancerState
        {
            public Dictionary<string, TargetGroup> TargetGroups = new Dictionary<string, TargetGroup>();
            public Dictionary<string, Amazon.ElasticLoadBalancingV2.Model.LoadBalancer> LoadBalancers = new Dictionary<string, Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>();
            public Dictionary<string, Listener> Listeners = new Dictionary<string, Listener>();
            public Dictionary<string, List<Rule>> RulesByListenerArn = new Dictionary<string, List<Rule>>();
        }
    }
}
