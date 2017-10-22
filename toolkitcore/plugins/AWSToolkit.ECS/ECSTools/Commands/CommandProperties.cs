using Amazon.ECS.Tools.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdParty.Json.LitJson;

namespace Amazon.ECS.Tools.Commands
{
    public class PushDockerImageProperties
    {
        public string Configuration { get; set; }
        public string TargetFramework { get; set; }
        public string DockerImageTag { get; set; }

        internal void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_CONFIGURATION.Switch)) != null)
                this.Configuration = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_FRAMEWORK.Switch)) != null)
                this.TargetFramework = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_DOCKER_TAG.Switch)) != null)
                this.DockerImageTag = tuple.Item2.StringValue;
        }


        internal void PersistSettings(BaseCommand command, JsonData data)
        {
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_CONFIGURATION.ConfigFileKey, command.GetStringValueOrDefault(this.Configuration, DefinedCommandOptions.ARGUMENT_CONFIGURATION, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_FRAMEWORK.ConfigFileKey, command.GetStringValueOrDefault(this.TargetFramework, DefinedCommandOptions.ARGUMENT_FRAMEWORK, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_DOCKER_TAG.ConfigFileKey, command.GetStringValueOrDefault(this.DockerImageTag, DefinedCommandOptions.ARGUMENT_DOCKER_TAG, false));
        }
    }


    public class ClusterProperties
    {
        public string ECSCluster { get; set; }

        internal void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_CLUSTER.Switch)) != null)
                this.ECSCluster = tuple.Item2.StringValue;
        }

        internal void PersistSettings(BaseCommand command, JsonData data)
        {
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_ECS_CLUSTER.ConfigFileKey, command.GetStringValueOrDefault(this.ECSCluster, DefinedCommandOptions.ARGUMENT_ECS_CLUSTER, false));
        }
    }


    public class TaskDefinitionProperties
    {
        public string ECSTaskDefinition { get; set; }
        public string ECSContainer { get; set; }
        public int? ContainerMemoryHardLimit { get; set; }
        public int? ContainerMemorySoftLimit { get; set; }
        public string TaskDefinitionRole { get; set; }
        public string[] PortMappings { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; }

        internal void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_TASK_DEFINITION.Switch)) != null)
                this.ECSTaskDefinition = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_CONTAINER.Switch)) != null)
                this.ECSContainer = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_MEMORY_HARD_LIMIT.Switch)) != null)
                this.ContainerMemoryHardLimit = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_MEMORY_SOFT_LIMIT.Switch)) != null)
                this.ContainerMemorySoftLimit = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_CONTAINER_PORT_MAPPING.Switch)) != null)
                this.PortMappings = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_TASK_DEFINITION_ROLE.Switch)) != null)
                this.TaskDefinitionRole = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES.Switch)) != null)
                this.EnvironmentVariables = tuple.Item2.KeyValuePairs;
        }

        internal void PersistSettings(BaseCommand command, JsonData data)
        {
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_ECS_TASK_DEFINITION.ConfigFileKey, command.GetStringValueOrDefault(this.ECSTaskDefinition, DefinedCommandOptions.ARGUMENT_ECS_TASK_DEFINITION, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_ECS_CONTAINER.ConfigFileKey, command.GetStringValueOrDefault(this.ECSContainer, DefinedCommandOptions.ARGUMENT_ECS_CONTAINER, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_ECS_MEMORY_HARD_LIMIT.ConfigFileKey, command.GetIntValueOrDefault(this.ContainerMemoryHardLimit, DefinedCommandOptions.ARGUMENT_ECS_MEMORY_HARD_LIMIT, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_ECS_MEMORY_SOFT_LIMIT.ConfigFileKey, command.GetIntValueOrDefault(this.ContainerMemorySoftLimit, DefinedCommandOptions.ARGUMENT_ECS_MEMORY_SOFT_LIMIT, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_TASK_DEFINITION_ROLE.ConfigFileKey, command.GetStringValueOrDefault(this.TaskDefinitionRole, DefinedCommandOptions.ARGUMENT_ECS_CONTAINER, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_ECS_CONTAINER_PORT_MAPPING.ConfigFileKey, DockerToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.PortMappings, DefinedCommandOptions.ARGUMENT_ECS_CONTAINER_PORT_MAPPING, false)));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES.ConfigFileKey, DockerToolsDefaults.FormatKeyValue(command.GetKeyValuePairOrDefault(this.EnvironmentVariables, DefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES, false)));
        }
    }



    public class DeployServiceProperties
    {
        public bool SkipImagePush { get; set; }
        public string ECSService { get; set; }
        public int? DesiredCount { get; set; }


        public int? DeploymentMinimumHealthyPercent { get; set; }
        public int? DeploymentMaximumPercent { get; set; }


        public string ELBServiceRole { get; set; }
        public string ELBTargetGroup { get; set; }
        public int? ELBContainerPort { get; set; }

        internal void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH.Switch)) != null)
                this.SkipImagePush = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_SERVICE.Switch)) != null)
                this.ECSService = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT.Switch)) != null)
                this.DesiredCount = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ELB_SERVICE_ROLE.Switch)) != null)
                this.ELBServiceRole = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ELB_TARGET_GROUP_ARN.Switch)) != null)
                this.ELBTargetGroup = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ELB_CONTAINER_PORT.Switch)) != null)
                this.ELBContainerPort = tuple.Item2.IntValue;
        }

        internal void PersistSettings(BaseCommand command, JsonData data)
        {
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH.ConfigFileKey, command.GetBoolValueOrDefault(this.SkipImagePush, DefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_ECS_SERVICE.ConfigFileKey, command.GetStringValueOrDefault(this.ECSService, DefinedCommandOptions.ARGUMENT_ECS_SERVICE, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT.ConfigFileKey, command.GetIntValueOrDefault(this.DesiredCount, DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT, false));

            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_ELB_SERVICE_ROLE.ConfigFileKey, command.GetStringValueOrDefault(this.ELBServiceRole, DefinedCommandOptions.ARGUMENT_ECS_SERVICE, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_ELB_TARGET_GROUP_ARN.ConfigFileKey, command.GetStringValueOrDefault(this.ELBTargetGroup, DefinedCommandOptions.ARGUMENT_ECS_SERVICE, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_ELB_CONTAINER_PORT.ConfigFileKey, command.GetIntValueOrDefault(this.ELBContainerPort, DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT, false));
        }
    }


    public class DeployScheduledTaskProperties
    {
        public bool SkipImagePush { get; set; }
        public string ScheduleTaskRule { get; set; }
        public string ScheduleTaskRuleTarget { get; set; }
        public string ScheduleExpression { get; set; }
        public string CloudWatchEventIAMRole { get; set; }
        public int DesiredCount { get; set; }

        internal void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_NAME.Switch)) != null)
                this.ScheduleTaskRule = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_TARGET.Switch)) != null)
                this.ScheduleTaskRuleTarget = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_SCHEDULE_EXPRESSION.Switch)) != null)
                this.ScheduleExpression = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_CLOUDWATCHEVENT_ROLE.Switch)) != null)
                this.CloudWatchEventIAMRole = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT.Switch)) != null)
                this.DesiredCount = tuple.Item2.IntValue;
        }

        internal void PersistSettings(BaseCommand command, JsonData data)
        {
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_TARGET.ConfigFileKey, command.GetStringValueOrDefault(this.ScheduleTaskRule, DefinedCommandOptions.ARGUMENT_ECS_SERVICE, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_TARGET.ConfigFileKey, command.GetStringValueOrDefault(this.ScheduleTaskRuleTarget, DefinedCommandOptions.ARGUMENT_ECS_SERVICE, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_SCHEDULE_EXPRESSION.ConfigFileKey, command.GetStringValueOrDefault(this.ScheduleExpression, DefinedCommandOptions.ARGUMENT_ECS_SERVICE, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_CLOUDWATCHEVENT_ROLE.ConfigFileKey, command.GetStringValueOrDefault(this.CloudWatchEventIAMRole, DefinedCommandOptions.ARGUMENT_ECS_SERVICE, false));
            data.SetIfNotNull(DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT.ConfigFileKey, command.GetIntValueOrDefault(this.DesiredCount, DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT, false));
        }
    }
}
