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

        public bool IsFargateLaunch(IAWSWizard hostingWizard)
        {
            var launchType = hostingWizard[PublishContainerToAWSWizardProperties.LaunchType] as string;
            bool isFargate = string.Equals(launchType, LaunchType.FARGATE, StringComparison.OrdinalIgnoreCase);
            return isFargate;
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
                ECSTaskDefinition = hostingWizard[PublishContainerToAWSWizardProperties.TaskDefinition] as string,
                ECSContainer = hostingWizard[PublishContainerToAWSWizardProperties.Container] as string,
                ContainerMemoryHardLimit = hostingWizard[PublishContainerToAWSWizardProperties.MemoryHardLimit] as int?,
                ContainerMemorySoftLimit = hostingWizard[PublishContainerToAWSWizardProperties.MemorySoftLimit] as int?
            };

            if(hostingWizard[PublishContainerToAWSWizardProperties.PortMappings] is IList<PortMappingItem>)
            {
                var uiPortMapping = hostingWizard[PublishContainerToAWSWizardProperties.PortMappings] as IList<PortMappingItem>;
                string[] mappings = new string[uiPortMapping.Count];
                for (int i = 0; i < mappings.Length; i++)
                {
                    mappings[i] = $"{uiPortMapping[i].HostPort}:{uiPortMapping[i].ContainerPort}";
                }
                properties.PortMappings = mappings;
            }
            if (hostingWizard[PublishContainerToAWSWizardProperties.EnvironmentVariables] is IList<EnvironmentVariableItem>)
            {
                var uiEnv = hostingWizard[PublishContainerToAWSWizardProperties.EnvironmentVariables] as IList<EnvironmentVariableItem>;
                var variables = new Dictionary<string, string>();
                for (int i = 0; i < uiEnv.Count; i++)
                {
                    variables[uiEnv[i].Variable] = uiEnv[i].Value;
                }
                properties.EnvironmentVariables = variables;
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
                properties.SecurityGroupIds = hostingWizard[PublishContainerToAWSWizardProperties.LaunchSecurityGroups] as string[];
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
