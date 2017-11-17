using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.ECS.Tools.Commands;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
using Amazon.IdentityManagement.Model;
using System.Threading;
using Amazon.IdentityManagement;
using Amazon.ECS;
using Amazon.AWSToolkit.ECS.WizardPages;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class BaseWorker
    {
        protected ILog LOGGER = LogManager.GetLogger(typeof(BaseWorker));

        protected IDockerDeploymentHelper Helper { get; private set; }

        protected IAmazonIdentityManagementService _iamClient;

        public BaseWorker(IDockerDeploymentHelper helper, IAmazonIdentityManagementService iamClient)
        {
            this.Helper = helper;
            this._iamClient = iamClient;
        }

        public PushDockerImageProperties ConvertToPushDockerImageProperties(IAWSWizard hostingWizard)
        {
            var properties = new PushDockerImageProperties();

            properties.Configuration = hostingWizard[PublishContainerToAWSWizardProperties.Configuration] as string;

            var dockerRepository = hostingWizard[PublishContainerToAWSWizardProperties.DockerRepository] as string;
            var dockerTag = hostingWizard[PublishContainerToAWSWizardProperties.DockerTag] as string;
            var dockerImageTag = dockerRepository;
            if (!string.IsNullOrWhiteSpace(dockerTag))
                dockerImageTag += ":" + dockerTag;

            properties.DockerImageTag = dockerImageTag;

            return properties;
        }

        public TaskDefinitionProperties ConvertToTaskDefinitionProperties(IAWSWizard hostingWizard)
        {
            var properties = new TaskDefinitionProperties
            {
                TaskDefinitionName = hostingWizard[PublishContainerToAWSWizardProperties.TaskDefinition] as string,
                ContainerName = hostingWizard[PublishContainerToAWSWizardProperties.Container] as string,
                ContainerMemoryHardLimit = hostingWizard[PublishContainerToAWSWizardProperties.MemoryHardLimit] as int?,
                ContainerMemorySoftLimit = hostingWizard[PublishContainerToAWSWizardProperties.MemorySoftLimit] as int?
            };

            if(hostingWizard[PublishContainerToAWSWizardProperties.PortMappings] is IList<PortMappingItem>)
            {
                var uiPortMapping = hostingWizard[PublishContainerToAWSWizardProperties.PortMappings] as IList<PortMappingItem>;
                string[] mappings = new string[uiPortMapping.Count];
                for (int i = 0; i < mappings.Length; i++)
                {
                    var hostPort = hostingWizard.IsFargateLaunch() ? uiPortMapping[i].ContainerPort : uiPortMapping[i].HostPort;
                    mappings[i] = $"{hostPort}:{uiPortMapping[i].ContainerPort}";
                }
                properties.ContainerPortMappings = mappings;
            }
            if (hostingWizard[PublishContainerToAWSWizardProperties.EnvironmentVariables] is IList<EnvironmentVariableItem>)
            {
                var uiEnv = hostingWizard[PublishContainerToAWSWizardProperties.EnvironmentVariables] as IList<EnvironmentVariableItem>;
                var variables = new Dictionary<string, string>();
                for (int i = 0; i < uiEnv.Count; i++)
                {
                    variables[uiEnv[i].Variable] = uiEnv[i].Value;
                }
                properties.ContainerEnvironmentVariables = variables;
            }

            if (hostingWizard[PublishContainerToAWSWizardProperties.TaskRole] is Role)
            {
                properties.TaskDefinitionRole = ((Role)hostingWizard[PublishContainerToAWSWizardProperties.TaskRole]).Arn;
            }
            else if (hostingWizard[PublishContainerToAWSWizardProperties.TaskRoleManagedPolicy] != null)
            {
                properties.TaskDefinitionRole = this.CreateRole(hostingWizard);
                this.Helper.AppendUploadStatus(string.Format("Created IAM role {0} with managed policy {1}",
                    properties.TaskDefinitionRole,
                    ((ManagedPolicy)hostingWizard[PublishContainerToAWSWizardProperties.TaskRoleManagedPolicy]).PolicyName));

                this.Helper.AppendUploadStatus("Waiting for new IAM Role to propagate to AWS regions");
                Thread.Sleep(SLEEP_TIME_FOR_ROLE_PROPOGATION);
            }

            if(hostingWizard.IsFargateLaunch())
            {
                if(hostingWizard[PublishContainerToAWSWizardProperties.CreateNewTaskExecutionRole] is bool &&
                    ((bool)hostingWizard[PublishContainerToAWSWizardProperties.CreateNewTaskExecutionRole]))
                {
                    properties.TaskDefinitionExecutionRole = Amazon.Common.DotNetCli.Tools.RoleHelper.CreateRole(
                        this._iamClient, "ecsTaskExecutionRole", Amazon.Common.DotNetCli.Tools.Constants.ECS_TASKS_ASSUME_ROLE_POLICY, "CloudWatchLogsFullAccess", "AmazonEC2ContainerRegistryReadOnly");
                }
                else
                {
                    properties.TaskDefinitionExecutionRole = hostingWizard[PublishContainerToAWSWizardProperties.TaskExecutionRole] as string;
                }

                properties.TaskCPU = hostingWizard[PublishContainerToAWSWizardProperties.AllocatedTaskCPU] as string;
                properties.TaskMemory = hostingWizard[PublishContainerToAWSWizardProperties.AllocatedTaskMemory] as string;
            }


            return properties;
        }

        public DeployScheduledTaskProperties ConvertToDeployScheduledTaskProperties(IAWSWizard hostingWizard)
        {
            var properties = new DeployScheduledTaskProperties
            {
                ScheduleTaskRule = hostingWizard[PublishContainerToAWSWizardProperties.ScheduleTaskRuleName] as string,
                ScheduleTaskRuleTarget = hostingWizard[PublishContainerToAWSWizardProperties.ScheduleTaskRuleTarget] as string,
                ScheduleExpression = hostingWizard[PublishContainerToAWSWizardProperties.ScheduleExpression] as string,
                CloudWatchEventIAMRole = hostingWizard[PublishContainerToAWSWizardProperties.CloudWatchEventIAMRole] as string,
                DesiredCount = (int)hostingWizard[PublishContainerToAWSWizardProperties.DesiredCount],
            };

            return properties;
        }

        public DeployServiceProperties ConvertToDeployServiceProperties(IAWSWizard hostingWizard)
        {
            var properties = new DeployServiceProperties
            {
                ECSService = hostingWizard[PublishContainerToAWSWizardProperties.Service] as string,
                DesiredCount = ((int)hostingWizard[PublishContainerToAWSWizardProperties.DesiredCount]),
                DeploymentMaximumPercent = ((int)hostingWizard[PublishContainerToAWSWizardProperties.MaximumPercent]),
                DeploymentMinimumHealthyPercent = ((int)hostingWizard[PublishContainerToAWSWizardProperties.MinimumHealthy]),
            };

            properties.PlacementConstraints = hostingWizard[PublishContainerToAWSWizardProperties.PlacementConstraints] as string[];
            properties.PlacementStrategy = hostingWizard[PublishContainerToAWSWizardProperties.PlacementStrategy] as string[];

            return properties;
        }

        public DeployTaskProperties ConvertToDeployTaskProperties(IAWSWizard hostingWizard)
        {
            var properties = new DeployTaskProperties
            {
                TaskCount = ((int)hostingWizard[PublishContainerToAWSWizardProperties.DesiredCount]),
                TaskGroup = hostingWizard[PublishContainerToAWSWizardProperties.TaskGroup] as string
            };

            properties.PlacementConstraints = hostingWizard[PublishContainerToAWSWizardProperties.PlacementConstraints] as string[];
            properties.PlacementStrategy = hostingWizard[PublishContainerToAWSWizardProperties.PlacementStrategy] as string[];

            return properties;
        }

        public ClusterProperties ConvertToClusterProperties(IAWSWizard hostingWizard)
        {
            var properties = new ClusterProperties
            {
                ECSCluster = hostingWizard[PublishContainerToAWSWizardProperties.ClusterName] as string,
                LaunchType = hostingWizard[PublishContainerToAWSWizardProperties.LaunchType] as string,
            };

            if(string.Equals(properties.LaunchType, Amazon.ECS.LaunchType.FARGATE, StringComparison.OrdinalIgnoreCase))
            {
                properties.SubnetIds = hostingWizard[PublishContainerToAWSWizardProperties.LaunchSubnets] as string[];

                if(hostingWizard[PublishContainerToAWSWizardProperties.CreateNewSecurityGroup] != null && 
                    (bool)hostingWizard[PublishContainerToAWSWizardProperties.CreateNewSecurityGroup])
                {
                    using (var ec2Client = ECSWizardUtils.CreateEC2Client(hostingWizard))
                    {
                        var groupName = properties.ECSCluster + "-" + DateTime.Now.Ticks;
                        var vpcId = hostingWizard[PublishContainerToAWSWizardProperties.VpcId] as string;

                        this.Helper.AppendUploadStatus("Creating security group {0}", groupName);
                        var groupId = ec2Client.CreateSecurityGroup(new Amazon.EC2.Model.CreateSecurityGroupRequest
                        {
                            GroupName = groupName,
                            VpcId = vpcId,
                            Description = "Created from VSToolkit " + DateTime.Now.ToString()
                        }).GroupId;
                        this.Helper.AppendUploadStatus("... Created: {0}", groupId);

                        var authorizeRequest = new Amazon.EC2.Model.AuthorizeSecurityGroupIngressRequest
                        {
                            GroupId = groupId
                        };
                        this.Helper.AppendUploadStatus("Authorizing port 80 for new security group");
                        authorizeRequest.IpPermissions.Add(new Amazon.EC2.Model.IpPermission
                        {
                            FromPort = 80,
                            ToPort = 80,
                            IpProtocol = "tcp",
                            Ipv4Ranges = new List<Amazon.EC2.Model.IpRange> { new Amazon.EC2.Model.IpRange {CidrIp="0.0.0.0/0" } },
                            Ipv6Ranges = new List<Amazon.EC2.Model.Ipv6Range> { new Amazon.EC2.Model.Ipv6Range { CidrIpv6 = "::/0" } },
                        });
                        ec2Client.AuthorizeSecurityGroupIngress(authorizeRequest);

                        properties.SecurityGroupIds = new string[] { groupId };

                        hostingWizard[PublishContainerToAWSWizardProperties.CreateNewSecurityGroup] = false;
                        hostingWizard[PublishContainerToAWSWizardProperties.LaunchSecurityGroups] = properties.SecurityGroupIds;
                    }
                }
                else
                {
                    properties.SecurityGroupIds = hostingWizard[PublishContainerToAWSWizardProperties.LaunchSecurityGroups] as string[];
                }
            }

            return properties;
        }


        static readonly TimeSpan SLEEP_TIME_FOR_ROLE_PROPOGATION = TimeSpan.FromSeconds(15);
        private string CreateRole(IAWSWizard hostingWizard)
        {
            var newRole = IAMUtilities.CreateRole(this._iamClient, "ecs_execution_" + hostingWizard[PublishContainerToAWSWizardProperties.TaskDefinition], Amazon.Common.DotNetCli.Tools.Constants.ECS_TASKS_ASSUME_ROLE_POLICY);

            this.Helper.AppendUploadStatus("Created IAM Role {0}", newRole.RoleName);

            var policy = hostingWizard[PublishContainerToAWSWizardProperties.TaskRoleManagedPolicy] as ManagedPolicy;
            if (policy != null)
            {
                this._iamClient.AttachRolePolicy(new AttachRolePolicyRequest
                {
                    RoleName = newRole.RoleName,
                    PolicyArn = policy.Arn
                });
                this.Helper.AppendUploadStatus("Attach policy {0} to role {1}", policy.PolicyName, newRole.RoleName);
            }

            return newRole.Arn;
        }
    }
}
