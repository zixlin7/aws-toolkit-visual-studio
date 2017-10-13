using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amazon.ECS.Tools.Options
{
    /// <summary>
    /// This class defines all the possible options across all the commands. The individual commands will then
    /// references the options that are appropiate.
    /// </summary>
    public static class DefinedCommandOptions
    {
        public static readonly CommandOption ARGUMENT_DISABLE_INTERACTIVE =
            new CommandOption
            {
                Name = "Disable Interactive",
                Switch = "--disable-interactive",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "When set to true missing required parameters will not be prompted for"
            };

        public static readonly CommandOption ARGUMENT_AWS_PROFILE =
            new CommandOption
            {
                Name = "AWS Profile",
                Switch = "--profile",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Profile to use to look up AWS credentials, if not set environment credentials will be used"
            };

        public static readonly CommandOption ARGUMENT_AWS_PROFILE_LOCATION =
            new CommandOption
            {
                Name = "AWS Profile Location",
                Switch = "--profile-location",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Optional override to the search location for Profiles, points at a shared credentials file"
            };

        public static readonly CommandOption ARGUMENT_AWS_REGION =
            new CommandOption
            {
                Name = "AWS Region",
                Switch = "--region",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The region to connect to AWS services, if not set region will be detected from the environment"
            };


        public static readonly CommandOption ARGUMENT_PROJECT_LOCATION =
            new CommandOption
            {
                Name = "Project Location",
                ShortSwitch = "-pl",
                Switch = "--project-location",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The location of the project, if not set the current directory will be assumed"
            };
        public static readonly CommandOption ARGUMENT_CONFIGURATION =
            new CommandOption
            {
                Name = "Build Configuration",
                ShortSwitch = "-c",
                Switch = "--configuration",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Configuration to build with, for example Release or Debug",
            };
        public static readonly CommandOption ARGUMENT_FRAMEWORK =
            new CommandOption
            {
                Name = "Framework",
                ShortSwitch = "-f",
                Switch = "--framework",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Target framework to compile, for example netcoreapp1.0",
            };
        public static readonly CommandOption ARGUMENT_CONFIG_FILE =
            new CommandOption
            {
                Name = "Config File",
                ShortSwitch = "-cfg",
                Switch = "--config-file",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = $"Configuration file storing default values for command line arguments. Default is {DockerToolsDefaultsReader.DEFAULT_FILE_NAME}"
            };
        public static readonly CommandOption ARGUMENT_PERSIST_CONFIG_FILE =
            new CommandOption
            {
                Name = "Persist Config File",
                ShortSwitch = "-pcfg",
                Switch = "--persist-config-file",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = $"If true the arguments used for a successful deployment are persisted to a config file. Default config file is {DockerToolsDefaultsReader.DEFAULT_FILE_NAME}"
            };

        public static readonly CommandOption ARGUMENT_DOCKER_TAG =
            new CommandOption
            {
                Name = "Docker Image Tag",
                ShortSwitch = "-t",
                Switch = "--tag",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name and optionally a tag in the 'name:tag' format",
            };
        public static readonly CommandOption ARGUMENT_SKIP_IMAGE_PUSH =
            new CommandOption
            {
                Name = "Skip Image Push",
                ShortSwitch = "-sip",
                Switch = "--skip-image-push",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "Skip building and push an image to Amazon ECR",
            };
        public static readonly CommandOption ARGUMENT_ECS_TASK_DEFINITION =
            new CommandOption
            {
                Name = "ECS Task Definition Name",
                ShortSwitch = "-etd",
                Switch = "--ecs-task-definition",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name of the ECS Task Defintion to be created or updated.",
            };
        public static readonly CommandOption ARGUMENT_ECS_CONTAINER =
            new CommandOption
            {
                Name = "ECS Container Name",
                ShortSwitch = "-econt",
                Switch = "--ecs-container",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name of the Container in a Task Definition to be created/updated",
            };

        public static readonly CommandOption ARGUMENT_ECS_MEMORY_HARD_LIMIT =
            new CommandOption
            {
                Name = "Container Memory Hard Limit",
                ShortSwitch = "-emhl",
                Switch = "--container-memory-hard-limit",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The hard limit (in MiB) of memory to present to the container",
            };

        public static readonly CommandOption ARGUMENT_ECS_MEMORY_SOFT_LIMIT =
            new CommandOption
            {
                Name = "Container Memory Soft Limit",
                ShortSwitch = "-emsl",
                Switch = "--container-memory-soft-limit",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The soft limit (in MiB) of memory to reserve for the container.",
            };

        public static readonly CommandOption ARGUMENT_ECS_CLUSTER =
            new CommandOption
            {
                Name = "ECS Cluster Name",
                ShortSwitch = "-ec",
                Switch = "--ecs-cluster",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name of the ECS Cluster to run the docker image",
            };

        public static readonly CommandOption ARGUMENT_ECS_SERVICE =
            new CommandOption
            {
                Name = "ECS Service Name",
                ShortSwitch = "-ecs",
                Switch = "--ecs-cluster-service",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name of the service to run on the ECS Cluster",
            };

        public static readonly CommandOption ARGUMENT_ECS_DESIRED_COUNT =
            new CommandOption
            {
                Name = "Desired Count",
                ShortSwitch = "-edc",
                Switch = "--ecs-desired-count",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The number of instantiations of the task to place and keep running in your service. Default is 1.",
            };

        public static readonly CommandOption ARGUMENT_ECS_CONTAINER_PORT_MAPPING =
            new CommandOption
            {
                Name = "Container Port Mapping",
                ShortSwitch = "-cpm",
                Switch = "--container-port-mapping",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "The mapping of container ports to host ports. Format is <host-port>:<container-port>,<host-port>:<container-port>,...",
            };
        public static readonly CommandOption ARGUMENT_ENVIRONMENT_VARIABLES =
            new CommandOption
            {
                Name = "Environment Variables",
                ShortSwitch = "-ev",
                Switch = "--environment-variables",
                ValueType = CommandOption.CommandOptionValueType.KeyValuePairs,
                Description = "Environment variables for a container definition. Format is <key1>=<value1>;<key2>=<value2>"
            };
        public static readonly CommandOption ARGUMENT_TASK_DEFINITION_ROLE =
            new CommandOption
            {
                Name = "Task Definition Role",
                ShortSwitch = "-trole",
                Switch = "--task-role",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The IAM role that will provide AWS credentials for the containers in the Task Definition"
            };
        public static readonly CommandOption ARGUMENT_ELB_TARGET_ARN =
            new CommandOption
            {
                Name = "ELB Target ARN",
                ShortSwitch = "-etg",
                Switch = "--elb-target-group",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The full Amazon Resource Name (ARN) of the Elastic Load Balancing target group associated with a service. "
            };
        public static readonly CommandOption ARGUMENT_ELB_CONTAINER_PORT =
            new CommandOption
            {
                Name = "ELB Container Port",
                ShortSwitch = "-ecp",
                Switch = "--elb-container-port",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The port on the container to associate with the load balancer."
            };
        public static readonly CommandOption ARGUMENT_DEPLOYMENT_MAXIMUM_PERCENT =
            new CommandOption
            {
                Name = "Deployment Maximum Percent",
                ShortSwitch = "-ecp",
                Switch = "--deployment-maximum-percent",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The upper limit of the number of tasks that are allowed in the RUNNING or PENDING state in a service during a deployment."
            };
        public static readonly CommandOption ARGUMENT_DEPLOYMENT_MINIMUM_HEALTHY_PERCENT =
            new CommandOption
            {
                Name = "Deployment Minimum Healhy Percent",
                ShortSwitch = "-dmhp",
                Switch = "--deployment-minimum-healthy-percent",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The lower limit of the number of running tasks that must remain in the RUNNING state in a service during a deployment."
            };
        public static readonly CommandOption ARGUMENT_ELB_SERVICE_ROLE =
            new CommandOption
            {
                Name = "ELB Service Role",
                ShortSwitch = "-esr",
                Switch = "--elb-service-role",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The name or full Amazon Resource Name (ARN) of the IAM role that allows Amazon ECS to make calls to your load balancer on your behalf."
            };

    }
}