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

using Amazon.EC2;
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;
using System.Threading;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class ViewClusterController : FeatureController<ViewClusterModel>
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewClusterController));

        ViewClusterControl _control;

        IAmazonElasticLoadBalancingV2 _elbClient;
        IAmazonEC2 _ec2Client;
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
                this._elbClient = this.Account.CreateServiceClient<AmazonElasticLoadBalancingV2Client>
                    (RegionEndPointsManager.Instance.GetRegion(this.RegionSystemName).GetEndpoint(RegionEndPointsManager.ELB_SERVICE_NAME));
                this._ec2Client = this.Account.CreateServiceClient<AmazonEC2Client>
                    (RegionEndPointsManager.Instance.GetRegion(this.RegionSystemName).GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME));


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
             var controller = new DeleteServiceConfirmationController(this.ECSClient, this._elbClient, this._ec2Client, this.Model, service);
            if(controller.Execute())
            {
                this.Refresh();
            }
        }

        public bool SaveService(ServiceWrapper service)
        {
            var request = new UpdateServiceRequest
            {
                Cluster = this._clusterArn,
                Service = service.ServiceArn,
                DesiredCount = service.DesiredCount
            };

            if(service.DeploymentMaximumPercent.HasValue || service.DeploymentMinimumHealthyPercent.HasValue)
            {
                request.DeploymentConfiguration = new DeploymentConfiguration
                {
                    MaximumPercent = service.DeploymentMaximumPercent.GetValueOrDefault(),
                    MinimumHealthyPercent = service.DeploymentMinimumHealthyPercent.GetValueOrDefault()
                };
            }

            var response = this.ECSClient.UpdateService(request);

            service.LoadFrom(response.Service);

            return true;
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

            // We need to loop through the target group arns individual because there is a chance a service is pointing to a non existing ELB target group.
            // In that case the entire batch describe fails
            List<TargetGroup> targetGroupList = new List<TargetGroup>();
            foreach(var targetGroupArn in targetGroupArns)
            {
                var request = new DescribeTargetGroupsRequest();
                request.TargetGroupArns.Add(targetGroupArn);
                try
                {
                    targetGroupList.AddRange(this._elbClient.DescribeTargetGroups(request).TargetGroups);
                }
                catch(Exception e)
                {
                    LOGGER.Error("Error getting target group " + targetGroupArn, e);
                }
            }

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
            var targetGroupArns = new List<string>();
            this.Model.Services.Clear();

            if (serviceArns.Count > 0)
            {
                var services = this.ECSClient.DescribeServices(new DescribeServicesRequest
                {
                    Cluster = this.Model.Cluster.ClusterArn,
                    Services = serviceArns
                }).Services;

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
            }

            foreach(var service in this.Model.Services)
            {
                service.LoadingELB = true;
            }

            System.Threading.Tasks.Task.Run<ViewClusterModel.LoadBalancerState>(() => this.FetchLoadBalancerState(targetGroupArns)).ContinueWith(x =>
            {
                foreach (var service in this.Model.Services)
                {
                    service.LoadingELB = false;
                }

                if (x.Exception == null)
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
