using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.ECS.Tools.Commands;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class BaseWorker
    {
        protected ILog LOGGER = LogManager.GetLogger(typeof(BaseWorker));

        protected IDockerDeploymentHelper Helper { get; private set; }

        public BaseWorker(IDockerDeploymentHelper helper)
        {
            this.Helper = helper;
        }

        public PushDockerImageProperties ConvertToPushDockerImageProperties(IAWSWizard hostWizard)
        {
            var properties = new PushDockerImageProperties();

            properties.Configuration = hostWizard[PublishContainerToAWSWizardProperties.Configuration] as string;

            var dockerRepository = hostWizard[PublishContainerToAWSWizardProperties.DockerRepository] as string;
            var dockerTag = hostWizard[PublishContainerToAWSWizardProperties.DockerTag] as string;
            var dockerImageTag = dockerRepository;
            if (!string.IsNullOrWhiteSpace(dockerTag))
                dockerImageTag += ":" + dockerTag;

            properties.DockerImageTag = dockerImageTag;

            return properties;
        }

        public TaskDefinitionProperties ConvertToTaskDefinitionProperties(IAWSWizard hostWizard)
        {
            var properties = new TaskDefinitionProperties
            {
                ECSTaskDefinition = hostWizard[PublishContainerToAWSWizardProperties.TaskDefinition] as string,
                ECSContainer = hostWizard[PublishContainerToAWSWizardProperties.Container] as string,
                ContainerMemoryHardLimit = hostWizard[PublishContainerToAWSWizardProperties.MemoryHardLimit] as int?,
                ContainerMemorySoftLimit = hostWizard[PublishContainerToAWSWizardProperties.MemorySoftLimit] as int?
            };

            if(hostWizard[PublishContainerToAWSWizardProperties.PortMappings] is IList<PortMappingItem>)
            {
                var uiPortMapping = hostWizard[PublishContainerToAWSWizardProperties.PortMappings] as IList<PortMappingItem>;
                string[] mappings = new string[uiPortMapping.Count];
                for (int i = 0; i < mappings.Length; i++)
                {
                    mappings[i] = $"{uiPortMapping[i].HostPort}:{uiPortMapping[i].ContainerPort}";
                }
                properties.PortMappings = mappings;
            }
            if (hostWizard[PublishContainerToAWSWizardProperties.EnvironmentVariables] is IList<EnvironmentVariableItem>)
            {
                var uiEnv = hostWizard[PublishContainerToAWSWizardProperties.EnvironmentVariables] as IList<EnvironmentVariableItem>;
                var variables = new Dictionary<string, string>();
                for (int i = 0; i < uiEnv.Count; i++)
                {
                    variables[uiEnv[i].Variable] = uiEnv[i].Value;
                }
                properties.EnvironmentVariables = variables;
            }

            return properties;
        }

        public DeployScheduledTaskProperties ConvertToDeployScheduledTaskProperties(IAWSWizard hostWizard)
        {
            var properties = new DeployScheduledTaskProperties
            {
                ScheduleTaskRule = hostWizard[PublishContainerToAWSWizardProperties.ScheduleTaskRuleName] as string,
                ScheduleTaskRuleTarget = hostWizard[PublishContainerToAWSWizardProperties.ScheduleTaskRuleTarget] as string,
                ScheduleExpression = hostWizard[PublishContainerToAWSWizardProperties.ScheduleExpression] as string,
                CloudWatchEventIAMRole = hostWizard[PublishContainerToAWSWizardProperties.CloudWatchEventIAMRole] as string,
                DesiredCount = (int)hostWizard[PublishContainerToAWSWizardProperties.DesiredCount],
            };

            return properties;
        }

        public DeployServiceProperties ConvertToDeployServiceProperties(IAWSWizard hostWizard)
        {
            var properties = new DeployServiceProperties
            {
                ECSService = hostWizard[PublishContainerToAWSWizardProperties.Service] as string,
                DesiredCount = ((int)hostWizard[PublishContainerToAWSWizardProperties.DesiredCount]),
                DeploymentMaximumPercent = ((int)hostWizard[PublishContainerToAWSWizardProperties.MaximumPercent]),
                DeploymentMinimumHealthyPercent = ((int)hostWizard[PublishContainerToAWSWizardProperties.MinimumHealthy]),
            };

            return properties;
        }

        public ClusterProperties ConvertToClusterProperties(IAWSWizard hostWizard)
        {
            var properties = new ClusterProperties
            {
                ECSCluster = hostWizard[PublishContainerToAWSWizardProperties.Cluster] as string
            };

            return properties;
        }
    }
}
