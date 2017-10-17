using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.ECS.View;
using Amazon.ECS.Model;
using log4net;

using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class ViewClusterController : FeatureController<ViewClusterModel>
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewClusterController));

        ViewClusterControl _control;

        IAmazonElasticLoadBalancingV2 _elbClient;
        string _clusterArn;

        protected override void DisplayView()
        {
            this._control = new ViewClusterControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            var clusterViewModel = this.FeatureViewModel as ClusterViewModel;
            if (clusterViewModel == null)
                throw new InvalidOperationException("Expected ClusterViewModel type for FeatureViewModel");

            try
            {
                var endpoint = RegionEndPointsManager.Instance.GetRegion(this.RegionSystemName).GetEndpoint(RegionEndPointsManager.ELB_SERVICE_NAME);
                this._elbClient = this.Account.CreateServiceClient<AmazonElasticLoadBalancingV2Client>(endpoint);

                this._clusterArn = clusterViewModel.Cluster.ClusterArn;

                this.Refresh();
            }
            catch (Exception e)
            {
                var msg = "Failed to query details for cluster with ARN " + clusterViewModel.Cluster.ClusterArn;
                LOGGER.Error(msg, e);
                ToolkitFactory.Instance.ShellProvider.ShowError(msg, "Resource Query Failure");
            }
        }

        public void Refresh()
        {
            var request = new DescribeClustersRequest
            {
                Clusters = new List<string>
                {
                    this._clusterArn
                }
            };
            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(AWSToolkit.Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
            var response = this.ECSClient.DescribeClusters(request);

            if (response.Clusters.Count != 1)
                throw new Exception("Failed to find cluster for ARN: " + this._clusterArn);

            if (this.Model.Cluster == null)
                this.Model.Cluster = new ClusterWrapper(response.Clusters[0]);
            else
                this.Model.Cluster.LoadFrom(response.Clusters[0]);

            this.RefreshServices();

        }

        class LoadBalancerState
        {
            public Dictionary<string, TargetGroup> TargetGroups = new Dictionary<string, TargetGroup>();
            public Dictionary<string, Amazon.ElasticLoadBalancingV2.Model.LoadBalancer> LoadBalancers = new Dictionary<string, Amazon.ElasticLoadBalancingV2.Model.LoadBalancer>();
            public Dictionary<string, Listener> Listeners = new Dictionary<string, Listener>();
            public Dictionary<string, List<Rule>> RulesByListenerArn = new Dictionary<string, List<Rule>>();
        }

        private LoadBalancerState FetchLoadBalancerState(List<string> targetGroupArns)
        {
            var state = new LoadBalancerState();

            var targetGroupList = this._elbClient.DescribeTargetGroups(new DescribeTargetGroupsRequest { TargetGroupArns = targetGroupArns }).TargetGroups;

            var loadBalancerArns = new List<string>();
            foreach (var targetGroup in targetGroupList)
            {
                state.TargetGroups[targetGroup.TargetGroupArn] = targetGroup;

                foreach (var loadBalancerArn in targetGroup.LoadBalancerArns)
                {
                    if (!loadBalancerArns.Contains(loadBalancerArn))
                    {
                        loadBalancerArns.Add(loadBalancerArn);
                    }
                }
            }

            var loadBalancerList = this._elbClient.DescribeLoadBalancers(new DescribeLoadBalancersRequest { LoadBalancerArns = loadBalancerArns }).LoadBalancers;

            foreach (var loadBalancer in loadBalancerList)
            {
                state.LoadBalancers[loadBalancer.LoadBalancerArn] = loadBalancer;
                var listenerList = this._elbClient.DescribeListeners(new DescribeListenersRequest { LoadBalancerArn = loadBalancer.LoadBalancerArn }).Listeners;
                foreach (var listener in listenerList)
                {
                    state.Listeners[listener.ListenerArn] = listener;
                }
            }

            foreach (var listener in state.Listeners.Values)
            {
                state.RulesByListenerArn[listener.ListenerArn] = this._elbClient.DescribeRules(new DescribeRulesRequest { ListenerArn = listener.ListenerArn }).Rules;
            }


            return state;
        }

        public void RefreshServices()
        {
            var serviceArns = this.ECSClient.ListServices(new ListServicesRequest { Cluster = this.Model.Cluster.ClusterArn }).ServiceArns;
            var services = this.ECSClient.DescribeServices(new DescribeServicesRequest
            {
                Cluster = this.Model.Cluster.ClusterArn,
                Services = serviceArns
            }).Services;

            var targetGroupArns = new List<string>();
            this.Model.Services.Clear();
            foreach (var service in services)
            {
                this.Model.Services.Add(new ServiceWrapper(service));

                foreach (var loadbalancer in service.LoadBalancers)
                {
                    if (string.IsNullOrEmpty(loadbalancer.TargetGroupArn) || targetGroupArns.Contains(loadbalancer.TargetGroupArn))
                        continue;

                    targetGroupArns.Add(loadbalancer.TargetGroupArn);
                }
            }

            System.Threading.Tasks.Task.Run<LoadBalancerState>(() => this.FetchLoadBalancerState(targetGroupArns)).ContinueWith(x =>
            {
                if(x.Exception == null)
                {
                    UpdateServicesWithLoadBalancerInfo(x.Result);
                }
            });
        }

        private void UpdateServicesWithLoadBalancerInfo(LoadBalancerState state)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((System.Action)(() =>
            {

                foreach (var service in this.Model.Services)
                {
                    if (string.IsNullOrEmpty(service.TargetGroupArn))
                        continue;

                    TargetGroup targetGroup;
                    if (!state.TargetGroups.TryGetValue(service.TargetGroupArn, out targetGroup))
                        continue;

                    Amazon.ElasticLoadBalancingV2.Model.LoadBalancer loadBalancer;
                    if (targetGroup.LoadBalancerArns.Count == 0 || !state.LoadBalancers.TryGetValue(targetGroup.LoadBalancerArns[0], out loadBalancer))
                        continue;

                    var loadBalancerListeners = state.Listeners.Values.Where(x => string.Equals(x.LoadBalancerArn, loadBalancer.LoadBalancerArn)).ToList();
                    if (loadBalancerListeners.Count == 0)
                        continue;

                    foreach (var listener in loadBalancerListeners)
                    {
                        var action = listener.DefaultActions.FirstOrDefault(x => string.Equals(targetGroup.TargetGroupArn, x.TargetGroupArn));

                        string pathPattern = null;
                        if (action != null)
                        {
                            pathPattern = "/";
                        }
                        else
                        {
                            List<Rule> listenerRules;
                            if (!state.RulesByListenerArn.TryGetValue(listener.ListenerArn, out listenerRules))
                                continue;

                            foreach (var rule in listenerRules)
                            {
                                action = rule.Actions.FirstOrDefault(x => string.Equals(targetGroup.TargetGroupArn, x.TargetGroupArn));
                                if (action != null)
                                {

                                    foreach (var condition in rule.Conditions)
                                    {
                                        if (string.Equals(condition.Field, "path-pattern", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            pathPattern = condition.Values[0];
                                            break;
                                        }
                                    }

                                    if (pathPattern == null)
                                        pathPattern = "/";

                                    break;
                                }
                            }
                        }

                        if (action != null && pathPattern != null)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.AppendFormat("{0}://{1}", listener.Protocol.ToString().ToLower(), loadBalancer.DNSName.ToLower());
                            if (listener.Port != 80)
                                sb.AppendFormat(":{0}", listener.Port);

                            sb.Append(pathPattern);
                            service.UploadLoadBalancerInfo(sb.ToString(), loadBalancer, listener, targetGroup);
                            break;
                        }
                    }
                }
            }));
        }
    }
}
