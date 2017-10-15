using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        static readonly TimeSpan SLEEP_TIME_FOR_ROLE_PROPOGATION = TimeSpan.FromSeconds(15);
        public void Execute(State state)
        {
            ConfigureLoadBalancerChangeTracker elbChanges = null;
            try
            {
                elbChanges = ConfigureLoadBalancer(state);
                if(!elbChanges.Success)
                {
                    throw new Exception(elbChanges.ErrorMessage);
                }



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
                    DeploymentMaximumPercent = state.DeploymentMaximumPercent,
                    DeploymentMinimumHealthyPercent = state.DeploymentMinimumHealthyPercent,

                    PersistConfigFile = state.PersistConfigFile
                };

                if (state.SelectedRole != null)
                {
                    command.TaskDefinitionRole = state.SelectedRole.Arn;
                }
                else
                {
                    command.TaskDefinitionRole = this.CreateRole(state);
                    this.Helper.AppendUploadStatus(string.Format("Created IAM role {0} with managed policy {1}", command.TaskDefinitionRole, state.SelectedManagedPolicy.PolicyName));
                    this.Helper.AppendUploadStatus("Waiting for new IAM Role to propagate to AWS regions");
                    Thread.Sleep(SLEEP_TIME_FOR_ROLE_PROPOGATION);
                }

                if (elbChanges.CreatedServiceIAMRole)
                {
                    command.ELBServiceRole = elbChanges.ServiceIAMRole;
                }
                else
                {
                    command.ELBServiceRole = state.ServiceIAMRole;
                }

                if (state.CreateNewTargetGroup)
                {
                    command.ELBTargetGroup = elbChanges.TargetGroup;

                    // TODO Figure out container port
                    command.ELBContainerPort = 80;
                }
                else if(!string.IsNullOrEmpty(state.TargetGroup))
                {
                    command.ELBTargetGroup = state.TargetGroup;

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

                if (state.EnvironmentVariables != null && state.EnvironmentVariables.Count > 0)
                {
                    var variables = new Dictionary<string, string>();
                    for (int i = 0; i < state.EnvironmentVariables.Count; i++)
                    {
                        variables[state.EnvironmentVariables[i].Variable] = state.EnvironmentVariables[i].Value;
                    }
                    command.EnvironmentVariables = variables;
                }

                if (command.ExecuteAsync().Result)
                {
                    this.Helper.SendCompleteSuccessAsync(state);
                }
                else
                {
                    CleanupELBResources(elbChanges);
                    if (command.LastToolsException != null)
                    {
                        this.Helper.SendCompleteErrorAsync("Error publishing container to AWS: " + command.LastToolsException.Message);
                    }
                    else
                    {
                        this.Helper.SendCompleteErrorAsync("Unknown error publishing container to AWS");
                    }
                }
            }
            catch (Exception e)
            {
                CleanupELBResources(elbChanges);
                LOGGER.Error("Error deploying to ECS Cluster.", e);
                this.Helper.SendCompleteErrorAsync("Error deploying to ECS Cluster: " + e.Message);
            }
        }

        private string CreateRole(State state)
        {
            var newRole = IAMUtilities.CreateRole(this._iamClient, "ecs_execution_" + state.TaskDefinition, Constants.ECS_TASKS_ASSUME_ROLE_POLICY);

            this.Helper.AppendUploadStatus("Created IAM Role {0}", newRole.RoleName);

            if (state.SelectedManagedPolicy != null)
            {
                this._iamClient.AttachRolePolicy(new AttachRolePolicyRequest
                {
                    RoleName = newRole.RoleName,
                    PolicyArn = state.SelectedManagedPolicy.Arn
                });
                this.Helper.AppendUploadStatus("Attach policy {0} to role {1}", state.SelectedManagedPolicy.PolicyName, newRole.RoleName);
            }

            return newRole.Arn;
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

        private string GenerateIAMRoleName(State state)
        {
            var baseName = "ecsServiceRole" + "-" + state.Cluster;

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

        private ConfigureLoadBalancerChangeTracker ConfigureLoadBalancer(State state)
        {
            var changeTracker = new ConfigureLoadBalancerChangeTracker();
            try
            {
                if (!state.ShouldConfigureELB)
                {
                    changeTracker.Success = true;
                    return changeTracker;
                }

                if (state.CreateNewIAMRole)
                {
                    var newRoleName = GenerateIAMRoleName(state);
                    CreateRoleRequest request = new CreateRoleRequest
                    {
                        RoleName = newRoleName,
                        AssumeRolePolicyDocument = Constants.ECS_ASSUME_ROLE_POLICY
                    };

                    changeTracker.ServiceIAMRole = this._iamClient.CreateRole(request).Role.Arn;
                    this.Helper.AppendUploadStatus("Created IAM service role for ECS to manage the load balancer: " + newRoleName);

                    this._iamClient.PutRolePolicy(new PutRolePolicyRequest
                    {
                        RoleName = request.RoleName,
                        PolicyName = "Default",
                        PolicyDocument = Constants.ECS_DEFAULT_SERVICE_POLICY
                    });
                    changeTracker.CreatedServiceIAMRole = true;
                    this.Helper.AppendUploadStatus("Added policy to IAM role {0} to give ECS permission to manage the load balancer", newRoleName);
                }
                else
                {
                    changeTracker.ServiceIAMRole = state.ServiceIAMRole;
                }

                var loadBalancerArn = state.LoadBalancer;
                if (state.CreateNewLoadBalancer)
                {
                    changeTracker.CreatedSecurityGroup = true;
                    changeTracker.SecurityGroup = CreateSecurityGroup(state);

                    changeTracker.AssignedSecurityGroups = AssignELBSecurityGroupToEC2SecurityGroup(state, changeTracker.SecurityGroup);

                    this.Helper.AppendUploadStatus("Creating Application Load Balancer");
                    loadBalancerArn = this._elbClient.CreateLoadBalancer(new CreateLoadBalancerRequest
                    {
                        Name = state.LoadBalancer,
                        IpAddressType = IpAddressType.Ipv4,
                        Scheme = LoadBalancerSchemeEnum.InternetFacing,
                        Type = LoadBalancerTypeEnum.Application,
                        SecurityGroups = new List<string> { changeTracker.SecurityGroup },
                        Subnets = DetermineSubnets(state),
                        Tags = new List<ElasticLoadBalancingV2.Model.Tag> { new ElasticLoadBalancingV2.Model.Tag { Key = "CreateSource", Value = "VSToolkitECSWizard" } }

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


                string listenerArn = state.ListenerArn;
                if (state.CreateNewListenerPort)
                {
                    string targetArn;
                    if (string.Equals(state.NewPathPattern, "/"))
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

                        changeTracker.CreateTargetGroup = true;
                        changeTracker.TargetGroup = targetArn;
                        this.Helper.AppendUploadStatus("New Target Group ARN:" + changeTracker.TargetGroup);
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

                        changeTracker.CreateDefaulTargetGroup = true;
                        changeTracker.DefaulTargetGroup = targetArn;
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

                    changeTracker.CreatedListener = true;
                    changeTracker.Listener = listenerArn;
                    this.Helper.AppendUploadStatus("New Listener ARN: " + listenerArn);

                    OpenListenerPort(state, changeTracker);
                }

                // elbTargetGroup could be already set as the default target for the listener
                if (state.CreateNewTargetGroup && changeTracker.TargetGroup == null)
                {
                    this.Helper.AppendUploadStatus("Creating TargetGroup for ELB Listener");
                    changeTracker.TargetGroup = this._elbClient.CreateTargetGroup(new CreateTargetGroupRequest
                    {
                        Name = makeTargetGroupNameUnique(state.TargetGroup),
                        Port = 80,
                        Protocol = ProtocolEnum.HTTP,
                        TargetType = TargetTypeEnum.Instance,
                        HealthCheckPath = state.HealthCheckPath,
                        VpcId = state.VpcId
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

                    var pathPattern = state.NewPathPattern;
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

            this.Helper.AppendUploadStatus("Authorizing access to port " + state.NewListenerPort + " for CidrIp 0.0.0.0/0");
            this._ec2Client.AuthorizeSecurityGroupIngress(new AuthorizeSecurityGroupIngressRequest
            {
                GroupId = groupId,
                IpPermissions = new List<IpPermission>
                {
                    new IpPermission
                    {
                        FromPort = state.NewListenerPort,
                        ToPort = state.NewListenerPort,
                        IpProtocol = "tcp",
                        Ipv4Ranges = new List<IpRange>{ new IpRange {CidrIp = "0.0.0.0/0" } }
                    }
                }
            });

            return groupId;
        }

        private void OpenListenerPort(State state, ConfigureLoadBalancerChangeTracker changes)
        {
            var loadBalancerArn = changes.CreatedLoadBalancer ? changes.LoadBalancer : state.LoadBalancer;
            var loadBalancers = this._elbClient.DescribeLoadBalancers(new DescribeLoadBalancersRequest { LoadBalancerArns = new List<string> { loadBalancerArn } }).LoadBalancers;
            if (loadBalancers.Count != 1)
                return;

            var loadBalancer = loadBalancers[0];
            changes.SecurityGroup = loadBalancer.SecurityGroups[0];
            try
            {
                this.Helper.AppendUploadStatus("Authorizing access to port " + state.NewListenerPort + " for CidrIp 0.0.0.0/0");
                this._ec2Client.AuthorizeSecurityGroupIngress(new AuthorizeSecurityGroupIngressRequest
                {
                    GroupId = loadBalancer.SecurityGroups[0],
                    IpPermissions = new List<IpPermission>
                    {
                        new IpPermission
                        {
                            FromPort = state.NewListenerPort,
                            ToPort = state.NewListenerPort,
                            IpProtocol = "tcp",
                            Ipv4Ranges = new List<IpRange>{ new IpRange {CidrIp = "0.0.0.0/0" } }
                        }
                    }
                });

                changes.AuthorizedListenerPort = state.NewListenerPort;
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

                foreach(var instanceSecurityGroup in elbChanges.AssignedSecurityGroups)
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
            public IList<EnvironmentVariableItem> EnvironmentVariables { get; set; }
            public Amazon.IdentityManagement.Model.Role SelectedRole { get; set; }
            public Amazon.IdentityManagement.Model.ManagedPolicy SelectedManagedPolicy { get; set; }

            public string Cluster { get; set; }
            public string Service { get; set; }
            public int DesiredCount { get; set; }
            public int DeploymentMinimumHealthyPercent { get; set; }
            public int DeploymentMaximumPercent { get; set; }

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
