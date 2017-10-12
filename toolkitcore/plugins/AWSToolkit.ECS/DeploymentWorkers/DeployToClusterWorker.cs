using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.EC2;
using Amazon.EC2.Model;

using Amazon.ECR;
using Amazon.ECS;

using Amazon.ElasticLoadBalancingV2;
using Amazon.ElasticLoadBalancingV2.Model;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using Amazon.ECS.Tools;
using Amazon.ECS.Tools.Commands;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
using Amazon.ECS.Model;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class DeployToClusterWorker : BaseWorker
    {
        IAmazonECR _ecrClient;
        IAmazonECS _ecsClient;
        IAmazonEC2 _ec2Client;
        IAmazonElasticLoadBalancingV2 _elbClient;
        IAmazonIdentityManagementService _iamClient;

        public DeployToClusterWorker(IDockerDeploymentHelper helper,
            IAmazonECR ecrClient,
            IAmazonECS ecsClient,
            IAmazonEC2 ec2Client,
            IAmazonElasticLoadBalancingV2 elbClient,
            IAmazonIdentityManagementService iamClient)
            : base(helper)
        {
            this._ecrClient = ecrClient;
            this._ecsClient = ecsClient;
            this._ec2Client = ec2Client;
            this._elbClient = elbClient;
            this._iamClient = iamClient;
        }

        public void Execute(State state)
        {
            try
            {
                var elbState = ConfigureLoadBalancer(state);

                var command = new DeployCommand(new ECSToolLogger(this.Helper), state.WorkingDirectory, new string[0])
                {
                    Profile = state.Account.Name,
                    Region = state.Region.SystemName,

                    DisableInteractive = true,
                    ECRClient = this._ecrClient,
                    ECSClient = this._ecsClient,

                    Configuration = state.Configuration,
                    DockerImageTag = state.DockerImageTag,

                    ECSTaskDefinition = state.TaskDefinition,
                    ECSContainer = state.Container,
                    ContainerMemoryHardLimit = state.MemoryHardLimit,
                    ContainerMemorySoftLimit = state.MemorySoftLimit,

                    ECSCluster = state.Cluster,
                    ECSService = state.Service,
                    DesiredCount = state.DesiredCount,

                    PersistConfigFile = state.PersistConfigFile
                };

                if (!string.IsNullOrWhiteSpace(elbState.TargetGroup))
                {
                    command.ELBServiceRole = elbState.ServiceRole;
                    command.ELBTargetGroup = elbState.TargetGroup;

                    // TODO Figure out container port
                    command.ELBContainerPort = 80;
                }

                if (state.PortMapping != null && state.PortMapping.Count > 0)
                {
                    string[] mappings = new string[state.PortMapping.Count];
                    for (int i = 0; i < mappings.Length; i++)
                    {
                        mappings[i] = $"{state.PortMapping[i].HostPort}:{state.PortMapping[i].ContainerPort}";
                    }
                    command.PortMappings = mappings;
                }

                if (command.ExecuteAsync().Result)
                {
                    this.Helper.SendCompleteSuccessAsync(state);
                }
                else
                {
                    if (command.LastToolsException != null)
                        this.Helper.SendCompleteErrorAsync("Error publishing container to AWS: " + command.LastToolsException.Message);
                    else
                    {
                        CleanupELBResources(elbState);
                        this.Helper.SendCompleteErrorAsync("Unknown error publishing container to AWS");
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deploying to ECS Cluster.", e);
                this.Helper.SendCompleteErrorAsync("Error deploying to ECS Cluster: " + e.Message);
            }
        }

        public class ConfigureLoadBalancerState
        {
            public bool CreatedServiceRole { get; set; }
            public string ServiceRole { get; set; }

            public bool CreatedSecurityGroup { get; set; }
            public string SecurityGroup { get; set; }
            public List<string> AssignedSecurityGroups { get; set; }

            public bool CreatedLoadBalancer { get; set; }
            public string LoadBalancer { get; set; }

            public bool CreatedListener { get; set; }
            public string Listener { get; set; }

            public bool CreatedRule { get; set; }
            public string Rule { get; set; }

            public bool CreateTargetGroup { get; set; }
            public string TargetGroup { get; set; }

            public bool CreateDefaulTargetGroup { get; set; }
            public string DefaulTargetGroup { get; set; }

        }

        private ConfigureLoadBalancerState ConfigureLoadBalancer(State state)
        {
            var elbState = new ConfigureLoadBalancerState();

            if (!state.ShouldConfigureELB)
                return elbState;

            if (state.CreateNewIAMRole)
            {
                // TODO Create role
            }
            else
            {
                elbState.ServiceRole = state.ServiceIAMRole;
            }

            var loadBalancerArn = state.LoadBalancer;
            if (state.CreateNewLoadBalancer)
            {
                elbState.CreatedSecurityGroup = true;
                elbState.SecurityGroup = CreateSecurityGroup(state);

                elbState.AssignedSecurityGroups = AssignELBSecurityGroupToEC2SecurityGroup(state, elbState.SecurityGroup);

                this.Helper.AppendUploadStatus("Creating Application Load Balancer");
                loadBalancerArn = this._elbClient.CreateLoadBalancer(new CreateLoadBalancerRequest
                {
                    Name = state.LoadBalancer,
                    IpAddressType = IpAddressType.Ipv4,
                    Scheme = LoadBalancerSchemeEnum.InternetFacing,
                    Type = LoadBalancerTypeEnum.Application,
                    SecurityGroups = new List<string> { elbState.SecurityGroup },
                    Subnets = DetermineSubnets(state),
                    Tags = new List<ElasticLoadBalancingV2.Model.Tag> { new ElasticLoadBalancingV2.Model.Tag {Key = "CreateSource", Value = "VSToolkitECSWizard" } }

                }).LoadBalancers[0].LoadBalancerArn;
                this.Helper.AppendUploadStatus("New Application Load Balancer ARN: " + loadBalancerArn);

                elbState.CreatedLoadBalancer = true;
                elbState.LoadBalancer = loadBalancerArn;
            }

            HashSet<string> existingTargetGroupNames = null;
            Func<string, string> makeTargetGroupNameUnique = baseName =>
            {
                if(existingTargetGroupNames == null)
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


            string listenerArn = state.ListenerArn;
            if(state.CreateNewListenerPort)
            {
                string targetArn;
                if(string.Equals(state.NewPathPattern, "/"))
                {
                    this.Helper.AppendUploadStatus("Creating TargetGroup for ELB Listener");
                    targetArn = this._elbClient.CreateTargetGroup(new CreateTargetGroupRequest
                    {
                        Name = makeTargetGroupNameUnique(state.TargetGroup),
                        Port = 80,
                        Protocol = ProtocolEnum.HTTP,
                        TargetType = TargetTypeEnum.Instance,
                        HealthCheckPath = state.HealthCheckPath,
                        VpcId = state.VpcId
                    }).TargetGroups[0].TargetGroupArn;

                    elbState.CreateTargetGroup = true;
                    elbState.TargetGroup = targetArn;
                    this.Helper.AppendUploadStatus("New Target Group ARN:" + elbState.TargetGroup);
                }
                else
                {
                    this.Helper.AppendUploadStatus("Creating default TargetGroup for ELB Listener");
                    targetArn = this._elbClient.CreateTargetGroup(new CreateTargetGroupRequest
                    {
                        Name = makeTargetGroupNameUnique("Default-ECS-" + state.Cluster),
                        Port = 80,
                        Protocol = ProtocolEnum.HTTP,
                        TargetType = TargetTypeEnum.Instance,
                        HealthCheckPath = "/",
                        VpcId = state.VpcId
                    }).TargetGroups[0].TargetGroupArn;

                    elbState.CreateDefaulTargetGroup = true;
                    elbState.DefaulTargetGroup = targetArn;
                    this.Helper.AppendUploadStatus("Default Target Group ARN:" + targetArn);
                }

                this.Helper.AppendUploadStatus("Creating ELB Listener for port " + state.NewListenerPort);
                listenerArn = this._elbClient.CreateListener(new CreateListenerRequest
                {
                    LoadBalancerArn = loadBalancerArn,
                    Port = state.NewListenerPort,
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

                elbState.CreatedListener = true;
                elbState.Listener = listenerArn;
                this.Helper.AppendUploadStatus("New Listener ARN: " + listenerArn);
            }

            // elbTargetGroup could be already set as the default target for the listener
            if (state.CreateNewTargetGroup && elbState.TargetGroup == null)
            {
                this.Helper.AppendUploadStatus("Creating TargetGroup for ELB Listener");
                elbState.TargetGroup = this._elbClient.CreateTargetGroup(new CreateTargetGroupRequest
                {
                    Name = makeTargetGroupNameUnique(state.TargetGroup),
                    Port = 80,
                    Protocol = ProtocolEnum.HTTP,
                    TargetType = TargetTypeEnum.Instance,
                    HealthCheckPath = state.HealthCheckPath,
                    VpcId = state.VpcId
                }).TargetGroups[0].TargetGroupArn;
                this.Helper.AppendUploadStatus("New Target Group ARN:" + elbState.TargetGroup);
                elbState.CreateTargetGroup = true;

                this.Helper.AppendUploadStatus("Getting existing rules to determine new listener rule's priority");
                var existingRules =  this._elbClient.DescribeRules(new DescribeRulesRequest { ListenerArn = listenerArn }).Rules;
                int currentMaxPriority = 1;
                foreach(var rule in existingRules)
                {
                    int rulePri;
                    if(int.TryParse(rule.Priority, out rulePri))
                    {
                        if (rulePri > currentMaxPriority)
                            currentMaxPriority = rulePri;
                    }
                }

                var pathPattern = state.NewPathPattern;
                if(!pathPattern.EndsWith("*"))
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
                            TargetGroupArn = elbState.TargetGroup,
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

                elbState.CreatedRule = true;
                elbState.Rule = ruleArn;
                this.Helper.AppendUploadStatus("New Rule Arn: " + ruleArn);
            }

            return elbState;
        }

        private List<string> DetermineSubnets(State state)
        {
            this.Helper.AppendUploadStatus("Determing subnets for new Application Load Balancer");
            var allSubnets = this._ec2Client.DescribeSubnets(new DescribeSubnetsRequest
            {
                Filters = new List<Filter> { new Filter { Name = "vpc-id", Values = new List<string> { state.VpcId } } }
            }).Subnets;

            var perZones = new Dictionary<string, Subnet>();
            foreach(var subnet in allSubnets)
            {
                if (subnet.State == SubnetState.Available)
                {
                    perZones[subnet.AvailabilityZone] = subnet;
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

        private List<string> AssignELBSecurityGroupToEC2SecurityGroup(State state, string elbSecurityGroupId)
        {
            this.Helper.AppendUploadStatus("Determining security groups of EC2 instances in cluster");
            var containerInstanceArns = _ecsClient.ListContainerInstances(new ListContainerInstancesRequest
            {
                Cluster = state.Cluster
            }).ContainerInstanceArns;

            var containerInstances = _ecsClient.DescribeContainerInstances(new DescribeContainerInstancesRequest
            {
                Cluster = state.Cluster,
                ContainerInstances = containerInstanceArns
            }).ContainerInstances;

            var describeIntanceRequest = new DescribeInstancesRequest();
            containerInstances.ForEach(x => describeIntanceRequest.InstanceIds.Add(x.Ec2InstanceId));

            List<string> groupIds = new List<string>();
            var reservations = _ec2Client.DescribeInstances(describeIntanceRequest).Reservations;
            foreach (var reservation in reservations)
            {
                foreach (var instance in reservation.Instances)
                {
                    if (!string.IsNullOrWhiteSpace(instance.VpcId))
                    {
                        foreach(var securityGroup in instance.SecurityGroups)
                        {
                            this.Helper.AppendUploadStatus("\t" + securityGroup.GroupId);
                            groupIds.Add(securityGroup.GroupId);
                        }
                    }
                }
            }

            foreach(var groupId in groupIds)
            {
                this.Helper.AppendUploadStatus("Authorizing the ELB security group {0} to the EC2 instance security group {1}", elbSecurityGroupId, groupId);
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
            }

            return groupIds;
        }

        private string CreateSecurityGroup(State state)
        {
            this.Helper.AppendUploadStatus("Fetching existing security groups to determine a new unique security group name");
            var existingSecurityGroups = this._ec2Client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest
            {
                Filters = new List<Filter> { new Filter { Name = "vpc-id", Values = new List<string> { state.VpcId } } }
            }).SecurityGroups;

            var baseSecurityGroupName = "ecs-" + state.Cluster + "-load-balancer";
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
                VpcId = state.VpcId,
                GroupName = securityGroupName,
                Description = "Load Balancer create for the ECS Cluster " + state.Cluster
            }).GroupId;

            this.Helper.AppendUploadStatus("Authorizing access to port 80 for CidrIp 0.0.0.0/0");
            this._ec2Client.AuthorizeSecurityGroupIngress(new AuthorizeSecurityGroupIngressRequest
            {
                GroupId = groupId,
                IpPermissions = new List<IpPermission>
                {
                    new IpPermission
                    {
                        FromPort = 80,
                        ToPort = 80,
                        IpProtocol = "tcp",
                        Ipv4Ranges = new List<IpRange>{ new IpRange {CidrIp = "0.0.0.0/0" } }
                    }
                }
            });

            return groupId;
        }

        private void CleanupELBResources(ConfigureLoadBalancerState elbState)
        {
            this.Helper.AppendUploadStatus("Attempting to clean up any ELB resources created for the failed deployment");
            if(elbState.CreatedLoadBalancer)
            {
                try
                {
                    this._elbClient.DeleteLoadBalancer(new DeleteLoadBalancerRequest { LoadBalancerArn = elbState.LoadBalancer });
                    this.Helper.AppendUploadStatus("Deleted load balancer");
                }
                catch(Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to delete load balancer {0}: {1}", elbState.LoadBalancer, e.Message);
                }
            }
            else if(elbState.CreatedListener)
            {
                try
                {
                    this._elbClient.DeleteListener(new DeleteListenerRequest { ListenerArn = elbState.Listener });
                    this.Helper.AppendUploadStatus("Deleted listener");
                }
                catch (Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to delete listener {0}: {1}", elbState.Listener, e.Message);
                }
            }

            if(elbState.CreatedSecurityGroup)
            {
                var ipPermissions = new List<IpPermission>
                {
                    new IpPermission
                    {
                            IpProtocol = "-1",
                            UserIdGroupPairs = new List<UserIdGroupPair>{ new UserIdGroupPair { GroupId = elbState.SecurityGroup } }
                    }
                };

                foreach(var instanceSecurityGroup in elbState.AssignedSecurityGroups)
                {
                    try
                    {
                        this._ec2Client.RevokeSecurityGroupIngress(new RevokeSecurityGroupIngressRequest { GroupId = instanceSecurityGroup, IpPermissions = ipPermissions });
                        this.Helper.AppendUploadStatus("Revoke EC2 security group {0} access from {1}", elbState.SecurityGroup, instanceSecurityGroup);
                    }
                    catch (Exception e)
                    {
                        this.Helper.AppendUploadStatus("Failed to revoke EC2 security group {0} access from {1}: {2}", elbState.SecurityGroup, instanceSecurityGroup, e.Message);
                    }
                }

                try
                {
                    this._ec2Client.DeleteSecurityGroup(new DeleteSecurityGroupRequest { GroupId = elbState.SecurityGroup });
                    this.Helper.AppendUploadStatus("Deleted EC2 security group");
                }
                catch (Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to delete EC2 security group {0}: {1}", elbState.SecurityGroup, e.Message);
                }
            }

            if(elbState.CreatedRule && !elbState.CreatedListener)
            {
                try
                {
                    this._elbClient.DeleteRule(new DeleteRuleRequest { RuleArn = elbState.Rule });
                    this.Helper.AppendUploadStatus("Deleted listener rule");
                }
                catch (Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to delete listener rule {0}: {1}", elbState.Rule, e.Message);
                }
            }

            if(elbState.CreateTargetGroup)
            {
                try
                {
                    this._elbClient.DeleteTargetGroup(new DeleteTargetGroupRequest { TargetGroupArn = elbState.TargetGroup });
                    this.Helper.AppendUploadStatus("Deleted target group");
                }
                catch (Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to delete target group {0}: {1}", elbState.TargetGroup, e.Message);
                }
            }

            if (elbState.CreateDefaulTargetGroup)
            {
                try
                {
                    this._elbClient.DeleteTargetGroup(new DeleteTargetGroupRequest { TargetGroupArn = elbState.DefaulTargetGroup });
                    this.Helper.AppendUploadStatus("Deleted default target group");
                }
                catch (Exception e)
                {
                    this.Helper.AppendUploadStatus("Failed to delete default target group {0}: {1}", elbState.DefaulTargetGroup, e.Message);
                }
            }
        }


        public class State
        {
            public AccountViewModel Account { get; set; }
            public RegionEndPointsManager.RegionEndPoints Region { get; set; }

            public string Configuration { get; set; }
            public string WorkingDirectory { get; set; }
            public string DockerImageTag { get; set; }

            public string TaskDefinition { get; set; }
            public string Container { get; set; }
            public int? MemoryHardLimit { get; set; }
            public int? MemorySoftLimit { get; set; }
            public IList<PortMappingItem> PortMapping { get; set; }

            public string Cluster { get; set; }
            public string Service { get; set; }
            public int DesiredCount { get; set; }

            public bool? PersistConfigFile { get; set; }

            public string VpcId { get; set; }
            public bool ShouldConfigureELB { get; set; }
            public bool CreateNewIAMRole { get; set; }
            public string ServiceIAMRole { get; set; }
            public bool CreateNewLoadBalancer { get; set; }
            public string LoadBalancer { get; set; }
            public bool CreateNewListenerPort { get; set; }
            public int NewListenerPort { get; set; }
            public string ListenerArn { get; set; }
            public bool CreateNewTargetGroup { get; set; }
            public string TargetGroup { get; set; }
            public string NewPathPattern { get; set; }
            public string HealthCheckPath { get; set; }
        }
    }
}
