using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.Util;
using Amazon.AWSToolkit.ECS.WizardPages;
using Amazon.CloudWatchLogs;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.ECR;
using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.ECS.Tools.Commands;
using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class DeployServiceWorker : BaseWorker, IEcsDeploy
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(DeployServiceWorker));

        private readonly IAmazonECR _ecrClient;
        private readonly IAmazonECS _ecsClient;
        private readonly IAmazonEC2 _ec2Client;
        private readonly IAmazonElasticLoadBalancingV2 _elbClient;
        private readonly IAmazonCloudWatchLogs _cwlClient;

        public DeployServiceWorker(IDockerDeploymentHelper helper,
            IAmazonECR ecrClient,
            IAmazonECS ecsClient,
            IAmazonEC2 ec2Client,
            IAmazonElasticLoadBalancingV2 elbClient,
            IAmazonIdentityManagementService iamClient,
            IAmazonCloudWatchLogs cwlClient,
            ToolkitContext toolkitContext)
            : base(helper, iamClient, toolkitContext)
        {
            this._ecrClient = ecrClient;
            this._ecsClient = ecsClient;
            this._ec2Client = ec2Client;
            this._elbClient = elbClient;
            this._cwlClient = cwlClient;
        }

        public void Execute(EcsDeployState state)
        {
            Execute(state, this);
        }

        /// <summary>
        /// Overload is for use in tests
        /// </summary>
        public void Execute(EcsDeployState state, IEcsDeploy ecsDeploy)
        {
            ActionResults result = null;

            void Invoke() => result = DeployService(state, ecsDeploy);

            void Record(ITelemetryLogger telemetryLogger, double duration)
            {
                var connectionSettings = new AwsConnectionSettings(state.Account?.Identifier, state.Region);
                EmitTaskDeploymentMetric(connectionSettings, result, state.HostingWizard, duration);
            }

            ToolkitContext.TelemetryLogger.TimeAndRecord(Invoke, Record);
        }

        private ActionResults DeployService(EcsDeployState state, IEcsDeploy ecsDeploy)
        {
            try
            {
                var result = ecsDeploy.Deploy(state).Result;
                if (result.Success)
                {
                    Helper.SendCompleteSuccessAsync(state);
                    if (state.PersistConfigFile.GetValueOrDefault())
                    {
                        base.PersistDeploymentMode(state.HostingWizard);
                    }
                }
                else
                {
                    Helper.SendCompleteErrorAsync("ECS Service deployment failed");
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.Error("Error deploying ECS Service.", e);
                Helper.SendCompleteErrorAsync("Error deploying ECS Service: " + e.Message);
                return ActionResults.CreateFailed(e);
            }
        }

        public class ConfigureLoadBalancerChangeTracker
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }

            public bool CreatedServiceIAMRole { get; set; }
            public string ServiceIAMRole { get; set; }

            public bool CreatedSecurityGroup { get; set; }
            public string SecurityGroup { get; set; }
            public List<string> AssignedSecurityGroups { get; set; }

            public bool CreatedLoadBalancer { get; set; }
            public string LoadBalancer { get; set; }

            public bool CreatedListener { get; set; }
            public string Listener { get; set; }
            public int AuthorizedListenerPort { get; set; }


            public bool CreatedRule { get; set; }
            public string Rule { get; set; }

            public bool CreateTargetGroup { get; set; }
            public string TargetGroup { get; set; }

            public bool CreateDefaulTargetGroup { get; set; }
            public string DefaulTargetGroup { get; set; }

        }

        private string GenerateIAMRoleName(EcsDeployState state)
        {
            var baseName = "ecsServiceRole" + "-" + state.HostingWizard[PublishContainerToAWSWizardProperties.ClusterName];

            var existingRoleNames = new HashSet<string>();
            var response = new ListRolesResponse();
            do
            {
                var roles = this._iamClient.ListRoles(new ListRolesRequest { Marker = response.Marker }).Roles;
                roles.ForEach(x => existingRoleNames.Add(x.RoleName));

            } while (response.IsTruncated);

            if (!existingRoleNames.Contains(baseName))
                return baseName;

            for(int i = 1; true; i++)
            {
                var name = baseName + "-" + i;
                if (!existingRoleNames.Contains(name))
                    return name;
            }
        }

        private ConfigureLoadBalancerChangeTracker ConfigureLoadBalancer(EcsDeployState state)
        {
            var changeTracker = new ConfigureLoadBalancerChangeTracker();
            try
            {
                if (!(state.HostingWizard[PublishContainerToAWSWizardProperties.ShouldConfigureELB] is bool) ||
                    !((bool)state.HostingWizard[PublishContainerToAWSWizardProperties.ShouldConfigureELB]))
                {
                    changeTracker.Success = true;
                    return changeTracker;
                }

                if (state.HostingWizard[PublishContainerToAWSWizardProperties.CreateNewIAMRole] is bool &&
                    ((bool)state.HostingWizard[PublishContainerToAWSWizardProperties.CreateNewIAMRole]))
                {
                    var newRoleName = GenerateIAMRoleName(state);
                    CreateRoleRequest request = new CreateRoleRequest
                    {
                        RoleName = newRoleName,
                        AssumeRolePolicyDocument = Amazon.Common.DotNetCli.Tools.Constants.ECS_ASSUME_ROLE_POLICY
                    };

                    changeTracker.ServiceIAMRole = this._iamClient.CreateRole(request).Role.Arn;
                    this.Helper.AppendUploadStatus("Created IAM service role for ECS to manage the load balancer: " + newRoleName);

                    this._iamClient.PutRolePolicy(new PutRolePolicyRequest
                    {
                        RoleName = request.RoleName,
                        PolicyName = "Default",
                        PolicyDocument = Amazon.ECS.Tools.Constants.ECS_DEFAULT_SERVICE_POLICY
                    });
                    changeTracker.CreatedServiceIAMRole = true;
                    this.Helper.AppendUploadStatus("Added policy to IAM role {0} to give ECS permission to manage the load balancer", newRoleName);
                }
                else
                {
                    changeTracker.ServiceIAMRole = state.HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] as string;
                }

                var loadBalancerArn = state.HostingWizard[PublishContainerToAWSWizardProperties.LoadBalancer] as string;
                if (state.HostingWizard[PublishContainerToAWSWizardProperties.CreateNewLoadBalancer] is bool &&
                    ((bool)state.HostingWizard[PublishContainerToAWSWizardProperties.CreateNewLoadBalancer]))
                {
                    changeTracker.CreatedSecurityGroup = true;
                    changeTracker.SecurityGroup = CreateSecurityGroup(state);

                    changeTracker.AssignedSecurityGroups = AssignELBSecurityGroupToEC2SecurityGroup(state, changeTracker.SecurityGroup);

                    this.Helper.AppendUploadStatus("Creating Application Load Balancer");
                    loadBalancerArn = this._elbClient.CreateLoadBalancer(new CreateLoadBalancerRequest
                    {
                        Name = loadBalancerArn,
                        IpAddressType = ElasticLoadBalancingV2.IpAddressType.Ipv4,
                        Scheme = LoadBalancerSchemeEnum.InternetFacing,
                        Type = LoadBalancerTypeEnum.Application,
                        SecurityGroups = new List<string> { changeTracker.SecurityGroup },
                        Subnets = DetermineSubnets(state),
                        Tags = new List<ElasticLoadBalancingV2.Model.Tag> { new ElasticLoadBalancingV2.Model.Tag { Key = Constants.WIZARD_CREATE_TAG_KEY, Value = Constants.WIZARD_CREATE_TAG_VALUE } }

                    }).LoadBalancers[0].LoadBalancerArn;
                    this.Helper.AppendUploadStatus("New Application Load Balancer ARN: " + loadBalancerArn);

                    changeTracker.CreatedLoadBalancer = true;
                    changeTracker.LoadBalancer = loadBalancerArn;
                }

                HashSet<string> existingTargetGroupNames = null;
                Func<string, string> makeTargetGroupNameUnique = baseName =>
                {
                    if (existingTargetGroupNames == null)
                    {
                        var targetGroups = this._elbClient.DescribeTargetGroups(new DescribeTargetGroupsRequest()).TargetGroups;
                        existingTargetGroupNames = new HashSet<string>();
                        foreach (var targetGroup in targetGroups)
                        {
                            existingTargetGroupNames.Add(targetGroup.TargetGroupName);
                        }

                    }

                    if (!existingTargetGroupNames.Contains(baseName))
                        return baseName;

                    for (int i = 1; true; i++)
                    {
                        var newName = baseName + "-" + i;
                        if (!existingTargetGroupNames.Contains(newName))
                            return newName;
                    }
                };

                var targetType = state.HostingWizard.IsFargateLaunch() ? TargetTypeEnum.Ip : TargetTypeEnum.Instance;

                string listenerArn = state.HostingWizard[PublishContainerToAWSWizardProperties.ListenerArn] as string;
                if (state.HostingWizard[PublishContainerToAWSWizardProperties.CreateNewListenerPort] is bool &&
                    ((bool)state.HostingWizard[PublishContainerToAWSWizardProperties.CreateNewListenerPort]))
                {
                    string targetArn;
                    if (state.HostingWizard[PublishContainerToAWSWizardProperties.NewPathPattern] != null && 
                        string.Equals(state.HostingWizard[PublishContainerToAWSWizardProperties.NewPathPattern].ToString(), "/"))
                    {
                        this.Helper.AppendUploadStatus("Creating TargetGroup for ELB Listener");

                        targetArn = this._elbClient.CreateTargetGroup(new CreateTargetGroupRequest
                        {
                            Name = makeTargetGroupNameUnique(state.HostingWizard[PublishContainerToAWSWizardProperties.TargetGroup] as string),
                            Port = 80,
                            Protocol = ProtocolEnum.HTTP,
                            TargetType = targetType,
                            HealthCheckPath = state.HostingWizard[PublishContainerToAWSWizardProperties.HealthCheckPath] as string,
                            VpcId = state.HostingWizard[PublishContainerToAWSWizardProperties.VpcId] as string
                        }).TargetGroups[0].TargetGroupArn;

                        changeTracker.CreateTargetGroup = true;
                        changeTracker.TargetGroup = targetArn;
                        this.Helper.AppendUploadStatus("New Target Group ARN:" + changeTracker.TargetGroup);
                    }
                    else
                    {
                        this.Helper.AppendUploadStatus("Creating default TargetGroup for ELB Listener");
                        targetArn = this._elbClient.CreateTargetGroup(new CreateTargetGroupRequest
                        {
                            Name = makeTargetGroupNameUnique("Default-ECS-" + state.HostingWizard[PublishContainerToAWSWizardProperties.ClusterName] as string),
                            Port = 80,
                            Protocol = ProtocolEnum.HTTP,
                            TargetType = targetType,
                            HealthCheckPath = "/",
                            VpcId = state.HostingWizard[PublishContainerToAWSWizardProperties.VpcId] as string
                        }).TargetGroups[0].TargetGroupArn;

                        changeTracker.CreateDefaulTargetGroup = true;
                        changeTracker.DefaulTargetGroup = targetArn;
                        this.Helper.AppendUploadStatus("Default Target Group ARN:" + targetArn);
                    }

                    this.Helper.AppendUploadStatus("Creating ELB Listener for port " + state.HostingWizard[PublishContainerToAWSWizardProperties.CreateNewListenerPort]);
                    listenerArn = this._elbClient.CreateListener(new CreateListenerRequest
                    {
                        LoadBalancerArn = loadBalancerArn,
                        Port = (int)state.HostingWizard[PublishContainerToAWSWizardProperties.NewListenerPort],
                        Protocol = ProtocolEnum.HTTP,
                        DefaultActions = new List<ElasticLoadBalancingV2.Model.Action>
                    {
                        new ElasticLoadBalancingV2.Model.Action
                        {
                            TargetGroupArn = targetArn,
                            Type = ActionTypeEnum.Forward
                        }
                    }
                    }).Listeners[0].ListenerArn;

                    changeTracker.CreatedListener = true;
                    changeTracker.Listener = listenerArn;
                    this.Helper.AppendUploadStatus("New Listener ARN: " + listenerArn);

                    OpenListenerPort(state, changeTracker);
                }

                // elbTargetGroup could be already set as the default target for the listener
                if (state.HostingWizard[PublishContainerToAWSWizardProperties.CreateNewTargetGroup] is bool &&
                    ((bool)state.HostingWizard[PublishContainerToAWSWizardProperties.CreateNewTargetGroup]) &&
                    changeTracker.TargetGroup == null)
                {
                    this.Helper.AppendUploadStatus("Creating TargetGroup for ELB Listener");
                    changeTracker.TargetGroup = this._elbClient.CreateTargetGroup(new CreateTargetGroupRequest
                    {
                        Name = makeTargetGroupNameUnique(state.HostingWizard[PublishContainerToAWSWizardProperties.TargetGroup] as string),
                        Port = 80,
                        Protocol = ProtocolEnum.HTTP,
                        TargetType = targetType,
                        HealthCheckPath = state.HostingWizard[PublishContainerToAWSWizardProperties.HealthCheckPath] as string,
                        VpcId = state.HostingWizard[PublishContainerToAWSWizardProperties.VpcId] as string
                    }).TargetGroups[0].TargetGroupArn;
                    this.Helper.AppendUploadStatus("New Target Group ARN:" + changeTracker.TargetGroup);
                    changeTracker.CreateTargetGroup = true;

                    this.Helper.AppendUploadStatus("Getting existing rules to determine new listener rule's priority");
                    var existingRules = this._elbClient.DescribeRules(new DescribeRulesRequest { ListenerArn = listenerArn }).Rules;
                    int currentMaxPriority = 1;
                    foreach (var rule in existingRules)
                    {
                        int rulePri;
                        if (int.TryParse(rule.Priority, out rulePri))
                        {
                            if (rulePri > currentMaxPriority)
                                currentMaxPriority = rulePri;
                        }
                    }

                    var pathPattern = state.HostingWizard[PublishContainerToAWSWizardProperties.NewPathPattern] as string;
                    if (!pathPattern.EndsWith("*"))
                    {
                        pathPattern += "*";
                    }

                    var newPriority = currentMaxPriority + 50;
                    this.Helper.AppendUploadStatus("Creating new listener rule for URL path " + pathPattern + " with priority " + newPriority);
                    var ruleArn = this._elbClient.CreateRule(new CreateRuleRequest
                    {
                        ListenerArn = listenerArn,
                        Actions = new List<ElasticLoadBalancingV2.Model.Action>
                    {
                        new ElasticLoadBalancingV2.Model.Action
                        {
                            TargetGroupArn = changeTracker.TargetGroup,
                            Type = ActionTypeEnum.Forward
                        }
                    },
                        Conditions = new List<RuleCondition>
                    {
                        new RuleCondition
                        {
                            Field = "path-pattern",
                            Values = new List<string>{ pathPattern }
                        }
                    },
                        Priority = newPriority
                    }).Rules[0].RuleArn;

                    changeTracker.CreatedRule = true;
                    changeTracker.Rule = ruleArn;
                    this.Helper.AppendUploadStatus("New Rule Arn: " + ruleArn);
                }

                changeTracker.Success = true;
            }
            catch(Exception e)
            {
                changeTracker.Success = false;
                changeTracker.ErrorMessage = e.Message;
            }

            return changeTracker;
        }

        private List<string> DetermineSubnets(EcsDeployState state)
        {
            this.Helper.AppendUploadStatus("Determing subnets for new Application Load Balancer");
            var allSubnets = this._ec2Client.DescribeSubnets(new DescribeSubnetsRequest
            {
                Filters = new List<Filter> { new Filter { Name = "vpc-id", Values = new List<string> { state.HostingWizard[PublishContainerToAWSWizardProperties.VpcId] as string } } }
            }).Subnets;

            var perZones = new Dictionary<string, Subnet>();
            foreach(var subnet in allSubnets)
            {
                if (!perZones.ContainsKey(subnet.AvailabilityZone) && subnet.MapPublicIpOnLaunch)
                {
                    if (subnet.State == SubnetState.Available)
                    {
                        perZones[subnet.AvailabilityZone] = subnet;
                    }
                }
            }

            var selectedSubnets = new List<string>();
            foreach(var kvp in perZones)
            {
                this.Helper.AppendUploadStatus("\t" + kvp.Key + " - " + kvp.Value.SubnetId);
                selectedSubnets.Add(kvp.Value.SubnetId);
            }

            return selectedSubnets;
        }

        private List<string> AssignELBSecurityGroupToEC2SecurityGroup(EcsDeployState state, string elbSecurityGroupId)
        {
            List<string> groupIds = new List<string>();

            if (state.HostingWizard.IsFargateLaunch())
            {
                var launchGroupsIds = state.HostingWizard[PublishContainerToAWSWizardProperties.LaunchSecurityGroups] as string[];
                groupIds = new List<string>(launchGroupsIds);
            }
            else
            {
                this.Helper.AppendUploadStatus("Determining security groups of EC2 instances in cluster");
                var containerInstanceArns = _ecsClient.ListContainerInstances(new ListContainerInstancesRequest
                {
                    Cluster = state.HostingWizard[PublishContainerToAWSWizardProperties.ClusterName] as string
                }).ContainerInstanceArns;

                var containerInstances = _ecsClient.DescribeContainerInstances(new DescribeContainerInstancesRequest
                {
                    Cluster = state.HostingWizard[PublishContainerToAWSWizardProperties.ClusterName] as string,
                    ContainerInstances = containerInstanceArns
                }).ContainerInstances;

                var describeIntanceRequest = new DescribeInstancesRequest();
                containerInstances.ForEach(x => describeIntanceRequest.InstanceIds.Add(x.Ec2InstanceId));

                var reservations = _ec2Client.DescribeInstances(describeIntanceRequest).Reservations;
                foreach (var reservation in reservations)
                {
                    foreach (var instance in reservation.Instances)
                    {
                        if (!string.IsNullOrWhiteSpace(instance.VpcId))
                        {
                            foreach (var securityGroup in instance.SecurityGroups)
                            {
                                this.Helper.AppendUploadStatus("\t" + securityGroup.GroupId);
                                groupIds.Add(securityGroup.GroupId);
                            }
                        }
                    }
                }
            }

            foreach(var groupId in groupIds)
            {
                try
                {
                    this._ec2Client.AuthorizeSecurityGroupIngress(new AuthorizeSecurityGroupIngressRequest
                    {
                        GroupId = groupId,
                        IpPermissions = new List<IpPermission>
                    {
                        new IpPermission
                        {
                            IpProtocol = "-1",
                            UserIdGroupPairs = new List<UserIdGroupPair>{ new UserIdGroupPair { GroupId = elbSecurityGroupId } }
                        }
                    }
                    });
                    this.Helper.AppendUploadStatus("Authorizing the ELB security group {0} to the EC2 instance security group {1}", elbSecurityGroupId, groupId);
                }
                catch(Exception e)
                {
                    this.Helper.AppendUploadStatus("Warning failed authorizing the ELB security group {0} to the EC2 instance security group {1}: {2}", elbSecurityGroupId, groupId, e.Message);
                }
            }

            return groupIds;
        }

        private string CreateSecurityGroup(EcsDeployState state)
        {
            this.Helper.AppendUploadStatus("Fetching existing security groups to determine a new unique security group name");
            var existingSecurityGroups = this._ec2Client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest
            {
                Filters = new List<Filter> { new Filter { Name = "vpc-id", Values = new List<string> { state.HostingWizard[PublishContainerToAWSWizardProperties.VpcId] as string } } }
            }).SecurityGroups;

            var baseSecurityGroupName = "ecs-" + state.HostingWizard[PublishContainerToAWSWizardProperties.ClusterName] + "-load-balancer";
            string securityGroupName = null;
            for (int i = 1; true; i++)
            {
                securityGroupName = baseSecurityGroupName + "-" + i;
                if (existingSecurityGroups.FirstOrDefault(x => string.Equals(securityGroupName, x.GroupName)) == null)
                    break;
            }

            this.Helper.AppendUploadStatus("Creating security group " + securityGroupName);
            var groupId = this._ec2Client.CreateSecurityGroup(new CreateSecurityGroupRequest
            {
                VpcId = state.HostingWizard[PublishContainerToAWSWizardProperties.VpcId] as string,
                GroupName = securityGroupName,
                Description = "Load Balancer created for the ECS Cluster " + state.HostingWizard[PublishContainerToAWSWizardProperties.ClusterName]
            }).GroupId;

            this.Helper.AppendUploadStatus("Tagging new security group");
            for (int i = 0; ; i++)
            {
                try
                {
                    this._ec2Client.CreateTags(new CreateTagsRequest
                    {
                        Resources = new List<string> { groupId },
                        Tags = new List<Amazon.EC2.Model.Tag> { new Amazon.EC2.Model.Tag { Key = Constants.WIZARD_CREATE_TAG_KEY, Value = Constants.WIZARD_CREATE_TAG_VALUE } }
                    });
                    break;
                }
                catch
                {
                    if(i >= 5)
                    {
                        throw;
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(i));
                }
            }

            this.Helper.AppendUploadStatus("Authorizing access to port " + state.HostingWizard[PublishContainerToAWSWizardProperties.NewListenerPort] + " for CidrIp 0.0.0.0/0");
            this._ec2Client.AuthorizeSecurityGroupIngress(new AuthorizeSecurityGroupIngressRequest
            {
                GroupId = groupId,
                IpPermissions = new List<IpPermission>
                {
                    new IpPermission
                    {
                        FromPort = (int)state.HostingWizard[PublishContainerToAWSWizardProperties.NewListenerPort],
                        ToPort = (int)state.HostingWizard[PublishContainerToAWSWizardProperties.NewListenerPort],
                        IpProtocol = "tcp",
                        Ipv4Ranges = new List<IpRange>{ new IpRange {CidrIp = "0.0.0.0/0" } }
                    }
                }
            });

            return groupId;
        }

        private void OpenListenerPort(EcsDeployState state, ConfigureLoadBalancerChangeTracker changes)
        {
            var loadBalancerArn = changes.CreatedLoadBalancer ? changes.LoadBalancer : state.HostingWizard[PublishContainerToAWSWizardProperties.LoadBalancer] as string;
            var loadBalancers = this._elbClient.DescribeLoadBalancers(new DescribeLoadBalancersRequest { LoadBalancerArns = new List<string> { loadBalancerArn } }).LoadBalancers;
            if (loadBalancers.Count != 1)
                return;

            var loadBalancer = loadBalancers[0];
            changes.SecurityGroup = loadBalancer.SecurityGroups[0];
            try
            {
                this.Helper.AppendUploadStatus("Authorizing access to port " + state.HostingWizard[PublishContainerToAWSWizardProperties.NewListenerPort] + " for CidrIp 0.0.0.0/0");
                this._ec2Client.AuthorizeSecurityGroupIngress(new AuthorizeSecurityGroupIngressRequest
                {
                    GroupId = loadBalancer.SecurityGroups[0],
                    IpPermissions = new List<IpPermission>
                    {
                        new IpPermission
                        {
                            FromPort = (int)state.HostingWizard[PublishContainerToAWSWizardProperties.NewListenerPort],
                            ToPort = (int)state.HostingWizard[PublishContainerToAWSWizardProperties.NewListenerPort],
                            IpProtocol = "tcp",
                            Ipv4Ranges = new List<IpRange>{ new IpRange {CidrIp = "0.0.0.0/0" } }
                        }
                    }
                });

                changes.AuthorizedListenerPort = (int)state.HostingWizard[PublishContainerToAWSWizardProperties.NewListenerPort];
            }
            catch(Exception e)
            {
                this.Helper.AppendUploadStatus("Warning, authorizing port: " + e.Message);
            }

            return;
        }

        private void CleanupELBResources(ConfigureLoadBalancerChangeTracker elbChanges)
        {
            this.Helper.AppendUploadStatus("Attempting to clean up any ELB resources created for the failed deployment");
            if(elbChanges.CreatedLoadBalancer)
            {
                try
                {
                    this._elbClient.DeleteLoadBalancer(new DeleteLoadBalancerRequest { LoadBalancerArn = elbChanges.LoadBalancer });
                    this.Helper.AppendUploadStatus("Deleted load balancer");

                    this.Helper.AppendUploadStatus("Wait for the eventual consistence of the delete so the connected resources can be deleted");
                    System.Threading.Thread.Sleep(3000);
                }
                catch(Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to delete load balancer {0}: {1}", elbChanges.LoadBalancer, e.Message);
                }
            }
            else if(elbChanges.CreatedListener)
            {
                try
                {
                    this._elbClient.DeleteListener(new DeleteListenerRequest { ListenerArn = elbChanges.Listener });
                    this.Helper.AppendUploadStatus("Deleted listener");

                    this.Helper.AppendUploadStatus("Wait for the eventual consistence of the delete so the connected resources can be deleted");
                    System.Threading.Thread.Sleep(3000);
                }
                catch (Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to delete listener {0}: {1}", elbChanges.Listener, e.Message);
                }
            }

            if(elbChanges.CreatedSecurityGroup)
            {
                var ipPermissions = new List<IpPermission>
                {
                    new IpPermission
                    {
                            IpProtocol = "-1",
                            UserIdGroupPairs = new List<UserIdGroupPair>{ new UserIdGroupPair { GroupId = elbChanges.SecurityGroup } }
                    }
                };

                if (elbChanges.AssignedSecurityGroups != null)
                {
                    foreach (var instanceSecurityGroup in elbChanges.AssignedSecurityGroups)
                    {
                        try
                        {
                            this._ec2Client.RevokeSecurityGroupIngress(new RevokeSecurityGroupIngressRequest { GroupId = instanceSecurityGroup, IpPermissions = ipPermissions });
                            this.Helper.AppendUploadStatus("Revoke EC2 security group {0} access from {1}", elbChanges.SecurityGroup, instanceSecurityGroup);
                        }
                        catch (Exception e)
                        {
                            this.Helper.AppendUploadStatus("Failed to revoke EC2 security group {0} access from {1}: {2}", elbChanges.SecurityGroup, instanceSecurityGroup, e.Message);
                        }
                    }
                }

                try
                {
                    this._ec2Client.DeleteSecurityGroup(new DeleteSecurityGroupRequest { GroupId = elbChanges.SecurityGroup });
                    this.Helper.AppendUploadStatus("Deleted EC2 security group");
                }
                catch (Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to delete EC2 security group {0}: {1}", elbChanges.SecurityGroup, e.Message);
                }
            }
            else if(elbChanges.AuthorizedListenerPort > 0)
            {
                try
                {
                    this._ec2Client.RevokeSecurityGroupIngress(new RevokeSecurityGroupIngressRequest
                    {
                        GroupId = elbChanges.SecurityGroup,
                        IpPermissions = new List<IpPermission>
                        {
                            new IpPermission
                            {
                                FromPort = elbChanges.AuthorizedListenerPort,
                                ToPort = elbChanges.AuthorizedListenerPort,
                                IpProtocol = "tcp",
                                Ipv4Ranges = new List<IpRange>{ new IpRange {CidrIp = "0.0.0.0/0" } }
                            }
                        }
                    });
                    this.Helper.AppendUploadStatus("Revoked access to port " + elbChanges.AuthorizedListenerPort + " for CidrIp 0.0.0.0/0");
                }
                catch (Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to revoke EC2 port {0} access from {1}: {2}", elbChanges.AuthorizedListenerPort, elbChanges.SecurityGroup, e.Message);
                }
            }

            if(elbChanges.CreatedRule && !elbChanges.CreatedListener)
            {
                try
                {
                    this._elbClient.DeleteRule(new DeleteRuleRequest { RuleArn = elbChanges.Rule });
                    this.Helper.AppendUploadStatus("Deleted listener rule");
                }
                catch (Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to delete listener rule {0}: {1}", elbChanges.Rule, e.Message);
                }
            }

            if(elbChanges.CreateTargetGroup)
            {
                try
                {
                    this._elbClient.DeleteTargetGroup(new DeleteTargetGroupRequest { TargetGroupArn = elbChanges.TargetGroup });
                    this.Helper.AppendUploadStatus("Deleted target group");
                }
                catch (Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to delete target group {0}: {1}", elbChanges.TargetGroup, e.Message);
                }
            }

            if (elbChanges.CreateDefaulTargetGroup)
            {
                try
                {
                    this._elbClient.DeleteTargetGroup(new DeleteTargetGroupRequest { TargetGroupArn = elbChanges.DefaulTargetGroup });
                    this.Helper.AppendUploadStatus("Deleted default target group");
                }
                catch (Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to delete default target group {0}: {1}", elbChanges.DefaulTargetGroup, e.Message);
                }
            }

            if(elbChanges.CreatedServiceIAMRole)
            {
                try
                {
                    var roleName = elbChanges.ServiceIAMRole;
                    if (roleName.Contains("/"))
                        roleName = roleName.Substring(roleName.IndexOf('/') + 1);


                    var policies = this._iamClient.ListRolePolicies(new ListRolePoliciesRequest { RoleName = roleName }).PolicyNames;
                    foreach(var policy in policies)
                    {
                        this._iamClient.DeleteRolePolicy(new DeleteRolePolicyRequest { RoleName = roleName, PolicyName = policy });
                        this.Helper.AppendUploadStatus("Deleted policy {0} from IAM service role {1}", policy, roleName);
                    }

                    this._iamClient.DeleteRole(new DeleteRoleRequest {RoleName = roleName });
                    this.Helper.AppendUploadStatus("Deleted IAM service role {0}", roleName);
                }
                catch (Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to delete IAM service role {0}: {1}", elbChanges.ServiceIAMRole, e.Message);
                }
            }
        }

        private void EmitTaskDeploymentMetric(AwsConnectionSettings connectionSettings, ActionResults result, IAWSWizard awsWizard, double duration)
        {
            try
            {
                var data = result.CreateMetricData<EcsDeployService>(connectionSettings,
                    ToolkitContext.ServiceClientManager);
                data.Result = result.AsTelemetryResult();
                data.EcsLaunchType = EcsTelemetryUtils.GetMetricsEcsLaunchType(awsWizard);
                data.Duration = duration;
                ToolkitContext.TelemetryLogger.RecordEcsDeployService(data);
            }
            catch (Exception e)
            {
                _logger.Error("Error logging metric", e);
                Debug.Assert(false, $"Unexpected error while logging deployment metric: {e.Message}");
            }
        }

        async Task<ActionResults> IEcsDeploy.Deploy(EcsDeployState state)
        {
            ConfigureLoadBalancerChangeTracker elbChanges = null;
            try
            {
                var credentials =
                    ToolkitContext.CredentialManager.GetAwsCredentials(state.Account.Identifier, state.Region);
                var command = new DeployServiceCommand(new ECSToolLogger(this.Helper), state.WorkingDirectory, new string[0])
                {
                    Profile = state.Account.Identifier.ProfileName,
                    Credentials = credentials,
                    Region = state.Region.Id,

                    DisableInteractive = true,
                    ECRClient = _ecrClient,
                    ECSClient = _ecsClient,
                    CWLClient = _cwlClient,
                    IAMClient = _iamClient,
                    EC2Client = _ec2Client,

                    PushDockerImageProperties = ConvertToPushDockerImageProperties(state.HostingWizard),
                    TaskDefinitionProperties = ConvertToTaskDefinitionProperties(state.HostingWizard),
                    DeployServiceProperties = ConvertToDeployServiceProperties(state.HostingWizard),
                    ClusterProperties = ConvertToClusterProperties(state.HostingWizard),

                    PersistConfigFile = state.PersistConfigFile
                };

                elbChanges = ConfigureLoadBalancer(state);
                if (!elbChanges.Success)
                {
                    throw new Exception(elbChanges.ErrorMessage);
                }

                if (elbChanges.CreatedServiceIAMRole)
                {
                    command.DeployServiceProperties.ELBServiceRole = elbChanges.ServiceIAMRole;
                }
                else
                {
                    command.DeployServiceProperties.ELBServiceRole = state.HostingWizard[PublishContainerToAWSWizardProperties.ServiceIAMRole] as string;
                }

                if ((state.HostingWizard[PublishContainerToAWSWizardProperties.ShouldConfigureELB] is bool) &&
                    ((bool)state.HostingWizard[PublishContainerToAWSWizardProperties.ShouldConfigureELB]))
                {
                    if (state.HostingWizard[PublishContainerToAWSWizardProperties.CreateNewTargetGroup] is bool &&
                        ((bool)state.HostingWizard[PublishContainerToAWSWizardProperties.CreateNewTargetGroup]))
                    {
                        command.DeployServiceProperties.ELBTargetGroup = elbChanges.TargetGroup;

                        // TODO Figure out container port
                        command.DeployServiceProperties.ELBContainerPort = 80;
                    }
                    else if (state.HostingWizard[PublishContainerToAWSWizardProperties.TargetGroup] != null &&
                        !string.IsNullOrEmpty(state.HostingWizard[PublishContainerToAWSWizardProperties.TargetGroup].ToString()))
                    {
                        command.DeployServiceProperties.ELBTargetGroup =
                            state.HostingWizard[PublishContainerToAWSWizardProperties.TargetGroup].ToString();

                        // TODO Figure out container port
                        command.DeployServiceProperties.ELBContainerPort = 80;
                    }
                }
                else
                {
                    command.OverrideIgnoreTargetGroup = true;
                }

                var result = await command.ExecuteAsync();

                if (!result)
                {
                    string errorContents = command.LastException?.Message ?? "Unknown";
                    string errorMessage = $"Error while deploying ECS Service to AWS: {errorContents}";

                    Helper.AppendUploadStatus(errorMessage);

                    CleanupELBResources(elbChanges);
                }

                var exception = DetermineErrorException(command.LastException, "Failed to deploy ECS service to AWS");
                return result ? new ActionResults().WithSuccess(true) : ActionResults.CreateFailed(exception);
            }
            catch (Exception e)
            {
                string errorMessage = $"Error while deploying ECS Service to AWS: {e.Message}";
                Helper.AppendUploadStatus(errorMessage);

                if (elbChanges != null)
                {
                    CleanupELBResources(elbChanges);
                }

                return ActionResults.CreateFailed(e);
            }
        }
    }
}
