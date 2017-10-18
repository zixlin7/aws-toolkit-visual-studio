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
        
        public void DeleteService(ServiceWrapper service)
        {
            TargetGroup targetGroup = null;
            

            if(!string.IsNullOrEmpty(service.TargetGroupArn))
            {
                this.Model.LBState.TargetGroups.TryGetValue(service.TargetGroupArn, out targetGroup);
            }

            Amazon.ElasticLoadBalancingV2.Model.LoadBalancer loadBalancer = null;
            if (targetGroup != null && targetGroup.LoadBalancerArns.Count == 1)
            {
                this.Model.LBState.LoadBalancers.TryGetValue(targetGroup.LoadBalancerArns[0], out loadBalancer);
            }

            List<Listener> listeners = null;
            if (loadBalancer != null)
            {
                this.Model.LBState.ListenersByLoadBalancerArn.TryGetValue(loadBalancer.LoadBalancerArn, out listeners);
            }

            Listener targetListener = null;

            bool canDeleteLoadBalancer = false;
            bool canDeleteListener = false;
            if (listeners.Count == 1 && SafeToDeleteListener(listeners[0], targetGroup))
            {
                canDeleteLoadBalancer = true;
            }
            else
            {
                foreach(var listener in listeners)
                {
                    if(SafeToDeleteListener(listener, targetGroup))
                    {
                        canDeleteListener = true;
                        targetListener = listener;
                        break;
                    }
                }
            }

            Rule targetRule = null;
            if(!canDeleteListener && !canDeleteLoadBalancer)
            {
                foreach(var listener in listeners)
                {
                    List<Rule> rules = null;
                    if(this.Model.LBState.RulesByListenerArn.TryGetValue(listener.ListenerArn, out rules))
                    {
                        foreach (var rule in rules)
                        {
                            if(string.Equals(rule.Actions[0].TargetGroupArn, targetGroup.TargetGroupArn))
                            {
                                targetRule = rule;
                                break;
                            }
                        }
                    }

                    if (targetRule != null)
                        break;
                }
            }

            var control = new DeleteServiceConfirmation(canDeleteLoadBalancer, loadBalancer, canDeleteListener, targetListener, targetGroup != null, targetGroup);
            if(ToolkitFactory.Instance.ShellProvider.ShowModal(control, System.Windows.MessageBoxButton.OKCancel))
            {

            }
        }

        private bool SafeToDeleteListener(Listener listener, TargetGroup targetGroup)
        {
            List<Rule> rules = null;
            this.Model.LBState.RulesByListenerArn.TryGetValue(listener.ListenerArn, out rules);

            // There is always the default rule
            if (rules == null || rules.Count == 1)
            {
                return string.Equals(listener.DefaultActions[0].TargetGroupArn, targetGroup.TargetGroupArn);
            }
            else if (rules.Count == 2)
            {
                var rule = rules.FirstOrDefault(x => !string.Equals(x.Priority, "default"));
                if (rule.Actions.Count == 1 && string.Equals(rule.Actions[0].TargetGroupArn, targetGroup.TargetGroupArn))
                {
                    var health = this._elbClient.DescribeTargetHealth(new DescribeTargetHealthRequest { TargetGroupArn = listener.DefaultActions[0].TargetGroupArn }).TargetHealthDescriptions;
                    if (health.Count == 0)
                        return true;
                }
            }


            return false;
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

        private ViewClusterModel.LoadBalancerState FetchLoadBalancerState(List<string> targetGroupArns)
        {
            var state = new ViewClusterModel.LoadBalancerState();

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
                state.ListenersByLoadBalancerArn[loadBalancer.LoadBalancerArn] = listenerList;

                foreach (var listener in listenerList)
                {
                    state.RulesByListenerArn[listener.ListenerArn] = this._elbClient.DescribeRules(new DescribeRulesRequest { ListenerArn = listener.ListenerArn }).Rules;
                }
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

            System.Threading.Tasks.Task.Run<ViewClusterModel.LoadBalancerState>(() => this.FetchLoadBalancerState(targetGroupArns)).ContinueWith(x =>
            {
                if(x.Exception == null)
                {
                    this.Model.LBState = x.Result;
                    UpdateServicesWithLoadBalancerInfo();
                }
            });
        }

        private void UpdateServicesWithLoadBalancerInfo()
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((System.Action)(() =>
            {

                foreach (var service in this.Model.Services)
                {
                    if (string.IsNullOrEmpty(service.TargetGroupArn))
                        continue;

                    TargetGroup targetGroup;
                    if (!this.Model.LBState.TargetGroups.TryGetValue(service.TargetGroupArn, out targetGroup))
                        continue;

                    Amazon.ElasticLoadBalancingV2.Model.LoadBalancer loadBalancer;
                    if (targetGroup.LoadBalancerArns.Count == 0 || !this.Model.LBState.LoadBalancers.TryGetValue(targetGroup.LoadBalancerArns[0], out loadBalancer))
                        continue;

                    var loadBalancerListeners = this.Model.LBState.ListenersByLoadBalancerArn[loadBalancer.LoadBalancerArn];
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
                            if (!this.Model.LBState.RulesByListenerArn.TryGetValue(listener.ListenerArn, out listenerRules))
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
