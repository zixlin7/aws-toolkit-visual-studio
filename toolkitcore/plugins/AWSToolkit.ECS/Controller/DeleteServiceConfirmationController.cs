using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.View;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Amazon.AWSToolkit.Context;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using LoadBalancer = Amazon.ElasticLoadBalancingV2.Model.LoadBalancer;
using TargetGroup = Amazon.ElasticLoadBalancingV2.Model.TargetGroup;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class DeleteServiceConfirmationController
    {
        IAmazonECS _ecsClient;
        IAmazonElasticLoadBalancingV2 _elbClient;
        IAmazonEC2 _ec2Client;
        private ViewClusterModel _viewClusterModel;
        private readonly ToolkitContext _toolkitContext;
        public string ClusterArn { get; set; }
        public ServiceWrapper Service { get; set; }
        public LoadBalancer LoadBalancer { get; set; }
        public Listener Listener { get; set; }
        public TargetGroup TargetGroup { get; set; }
        public Rule Rule { get; set; }

        public DeleteServiceConfirmationController(IAmazonECS ecsClient, IAmazonElasticLoadBalancingV2 elbClient, IAmazonEC2 ec2Client,
            ViewClusterModel viewClusterModel, ServiceWrapper service, ToolkitContext toolkitContext)
        {
            this._ecsClient = ecsClient;
            this._elbClient = elbClient;
            this._ec2Client = ec2Client;
            this._viewClusterModel = viewClusterModel;
            this.Service = service;
            this._toolkitContext = toolkitContext;
        }

        DeleteServiceConfirmation _control;
        public bool Execute()
        {
            DetermineELBResources();

            this._control = new DeleteServiceConfirmation(this);
            if (_toolkitContext.ToolkitHost.ShowModal(this._control, System.Windows.MessageBoxButton.OKCancel))
            {
                RecordEcsDeleteServiceMetric(Result.Succeeded);
                return true;
            }

            RecordEcsDeleteServiceMetric(Result.Failed);
            return false;
        }

        private void RecordEcsDeleteServiceMetric(Result result)
        {
            _toolkitContext.TelemetryLogger.RecordEcsDeleteService(new EcsDeleteService()
            {
                AwsAccount = _toolkitContext.ConnectionManager?.ActiveAccountId ?? MetadataValue.NotSet,
                AwsRegion = _toolkitContext.ConnectionManager?.ActiveRegion?.Id ?? MetadataValue.NotSet,
                Result = result
            });
        }

        public void DeleteService()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.DeleteServiceAsync));
        }

        private void CleanupEC2SecurityGroup(string vpcId, List<string> elbSecurityGroups)
        {
            try
            {
                // If there is more then one then the developer has done something custom
                if (elbSecurityGroups.Count != 1)
                {
                    this._control.AppendOutputMessage("Security Group was not setup by this tooling so skipping cleaning up Load Balancer security group");
                    return;
                }


                SecurityGroup elbSecurityGroup;
                try
                {
                    var response = this._ec2Client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest { GroupIds = elbSecurityGroups });
                    if (response.SecurityGroups.Count != 1)
                    {
                        this._control.AppendOutputMessage("Failed to find the load balancer security group {0}", elbSecurityGroups[0]);
                        return;
                    }
                    elbSecurityGroup = response.SecurityGroups[0];
                }
                catch(Exception)
                {
                    this._control.AppendOutputMessage("Failed to find the load balancer security group {0}", elbSecurityGroups[0]);
                    return;
                }

            

                var tag = elbSecurityGroup.Tags.FirstOrDefault(x => string.Equals(x.Key, Constants.WIZARD_CREATE_TAG_KEY) && string.Equals(x.Value, Constants.WIZARD_CREATE_TAG_VALUE));
                if(tag == null)
                {
                    this._control.AppendOutputMessage("Security Group was not setup by this tooling so skipping cleaning up Load Balancer security group");
                    return;
                }

                this._control.AppendOutputMessage("Fetching existing security groups to look for references to the Load Balancer security group");
                var existingSecurityGroups = this._ec2Client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest
                {
                    Filters = new List<Filter> { new Filter { Name = "vpc-id", Values = new List<string> { vpcId } } }
                }).SecurityGroups;

                bool performedRevoke = false;
                foreach(var securityGroup in existingSecurityGroups)
                {
                    if (string.Equals(securityGroup.GroupId, elbSecurityGroup.GroupId))
                        continue;
                
                    foreach(var permission in securityGroup.IpPermissions)
                    {
                        if(permission.UserIdGroupPairs != null && permission.UserIdGroupPairs.FirstOrDefault(x => string.Equals(x.GroupId, elbSecurityGroup.GroupId)) != null)
                        {
                            performedRevoke = true;
                            this._control.AppendOutputMessage("Revoke the load balancer security group {0} from security group {1}", elbSecurityGroup.GroupId, securityGroup.GroupId);
                            this._ec2Client.RevokeSecurityGroupIngress(new RevokeSecurityGroupIngressRequest
                            {
                                GroupId = securityGroup.GroupId,
                                IpPermissions = new List<IpPermission>
                                {
                                    new IpPermission
                                    {
                                        IpProtocol = permission.IpProtocol,
                                        UserIdGroupPairs = new List<UserIdGroupPair> {new UserIdGroupPair{GroupId = elbSecurityGroup.GroupId} }
                                    }
                                }
                            });
                        }
                    }
                }

                if (performedRevoke)
                {
                    this._control.AppendOutputMessage("Waiting for permission revokes to finish up");
                    Thread.Sleep(TimeSpan.FromSeconds(20));
                }

                this._control.AppendOutputMessage("Deleting load balancer security group {0}", elbSecurityGroup.GroupId);
                Exception deleteSecurityGroupException = null;
                for (int retries = 0; retries < 15; retries++)
                {
                    if(retries != 0)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }

                    deleteSecurityGroupException = null;
                    try
                    {
                        if (retries > 0)
                        {
                            this._control.AppendOutputMessage("... retry: " + retries);
                        }

                        this._ec2Client.DeleteSecurityGroup(new DeleteSecurityGroupRequest { GroupId = elbSecurityGroup.GroupId });
                        break;
                    }
                    catch (Exception e)
                    {
                        deleteSecurityGroupException = e;
                    }
                }

                if (deleteSecurityGroupException != null)
                {
                    this._control.AppendOutputMessage("Failed to deleting load balancer security group: {0}", deleteSecurityGroupException.Message);
                }
            }
            catch (Exception e)
            {
                this._control.AppendOutputMessage("Error cleanup load balancer security group: {0}", e.Message);
            }

        }

        private void DeleteServiceAsync(object state)
        {
            try
            {
                if (this._control.DeleteLoadbalancer)
                {
                    this._control.AppendOutputMessage("Deleting loadbalancer: " + this.LoadBalancer.LoadBalancerArn);
                    this._elbClient.DeleteLoadBalancer(new DeleteLoadBalancerRequest { LoadBalancerArn = this.LoadBalancer.LoadBalancerArn });
                    this._control.AppendOutputMessage("Waiting for load balancer deletion to finish up");
                    Thread.Sleep(TimeSpan.FromSeconds(8));

                    this.CleanupEC2SecurityGroup(this.LoadBalancer.VpcId, this.LoadBalancer.SecurityGroups);
                }
                else if (this._control.DeleteListener)
                {
                    this._control.AppendOutputMessage("Deleting listener: {0} ({1})", this.Listener.Port, this.Listener.Protocol);
                    this._elbClient.DeleteListener(new DeleteListenerRequest { ListenerArn = this.Listener.ListenerArn });
                    this._control.AppendOutputMessage("Waiting for listener deletion to finish up");
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }
                if (this._control.DeleteTargetGroup)
                {
                    if (this.Rule != null)
                    {
                        string ruleDisplayName;
                        if (Rule.Conditions.Count > 0 && this.Rule.Conditions[0].Values.Count > 0)
                            ruleDisplayName = string.Format("{0} - {1}", this.Rule.Conditions[0].Field, this.Rule.Conditions[0].Values[0]);
                        else
                            ruleDisplayName = this.Rule.RuleArn;
                        this._control.AppendOutputMessage("Deleting listener rule: " + ruleDisplayName);
                        this._elbClient.DeleteRule(new DeleteRuleRequest() { RuleArn = this.Rule.RuleArn });
                    }

                    this._control.AppendOutputMessage("Deleting target group: " + TargetGroup.TargetGroupArn);
                    this._elbClient.DeleteTargetGroup(new DeleteTargetGroupRequest { TargetGroupArn = this.TargetGroup.TargetGroupArn });
                }

                this._control.AppendOutputMessage("Reducing {0} service's desired count to 0", this.Service.ServiceName);
                this._ecsClient.UpdateService(new UpdateServiceRequest
                    {
                        Cluster = this._viewClusterModel.Cluster.ClusterArn,
                        Service = this.Service.ServiceArn,
                        DesiredCount = 0
                    });

                this._control.AppendOutputMessage("Deleting service: " + this.Service.ServiceName);
                this._ecsClient.DeleteService(new DeleteServiceRequest { Cluster = this._viewClusterModel.Cluster.ClusterArn, Service = this.Service.ServiceArn });

                this._control.DeleteAsyncComplete(true);
            }
            catch (Exception e)
            {
                this._control.AppendOutputMessage(e.Message);
                this._control.DeleteAsyncComplete(false);
            }

        }

        private void DetermineELBResources()
        {
            Amazon.ElasticLoadBalancingV2.Model.LoadBalancer loadBalancer = null;
            Listener targetListener = null;
            TargetGroup targetGroup = null;
            Rule targetRule = null;

            bool canDeleteLoadBalancer = false;
            bool canDeleteListener = false;
            bool isTargetGroupADefault = false;

            if (!string.IsNullOrEmpty(this.Service.TargetGroupArn))
            {
                this._viewClusterModel.LBState.TargetGroups.TryGetValue(this.Service.TargetGroupArn, out targetGroup);
            }
            if (targetGroup != null)
            {
                if (targetGroup != null && targetGroup.LoadBalancerArns.Count == 1)
                {
                    this._viewClusterModel.LBState.LoadBalancers.TryGetValue(targetGroup.LoadBalancerArns[0], out loadBalancer);
                }

                List<Listener> listeners = null;
                if (loadBalancer != null)
                {
                    this._viewClusterModel.LBState.ListenersByLoadBalancerArn.TryGetValue(loadBalancer.LoadBalancerArn, out listeners);
                }

                if (listeners != null)
                {
                    foreach(var listener in listeners)
                    {
                        isTargetGroupADefault = listener.DefaultActions.FirstOrDefault(x => string.Equals(x.TargetGroupArn, targetGroup.TargetGroupArn)) != null;
                        if (isTargetGroupADefault)
                            break;
                    }

                    if (listeners.Count == 1 && SafeToDeleteListener(listeners[0], targetGroup))
                    {
                        canDeleteLoadBalancer = true;
                    }
                    else
                    {
                        foreach (var listener in listeners)
                        {
                            if (SafeToDeleteListener(listener, targetGroup))
                            {
                                canDeleteListener = true;
                                targetListener = listener;
                                break;
                            }
                        }
                    }


                    if (!canDeleteListener && !canDeleteLoadBalancer)
                    {
                        foreach (var listener in listeners)
                        {
                            List<Rule> rules = null;
                            if (this._viewClusterModel.LBState.RulesByListenerArn.TryGetValue(listener.ListenerArn, out rules))
                            {
                                foreach (var rule in rules)
                                {
                                    if (string.Equals(rule.Actions[0].TargetGroupArn, targetGroup.TargetGroupArn))
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
                }
            }

            this.LoadBalancer = canDeleteLoadBalancer ? loadBalancer : null;
            this.Listener = canDeleteListener ? targetListener : null;
            this.TargetGroup = canDeleteLoadBalancer || canDeleteListener || !isTargetGroupADefault ? targetGroup : null;
            this.Rule = targetGroup != null ? targetRule : null;
        }


        private bool SafeToDeleteListener(Listener listener, TargetGroup targetGroup)
        {
            List<Rule> rules = null;
            this._viewClusterModel.LBState.RulesByListenerArn.TryGetValue(listener.ListenerArn, out rules);

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
                    var activeHealth = health.Where(x => x.TargetHealth.State != TargetHealthStateEnum.Draining && x.TargetHealth.State != TargetHealthStateEnum.Unused);
                    if (activeHealth.Count() == 0)
                        return true;
                }
            }


            return false;
        }
    }
}
