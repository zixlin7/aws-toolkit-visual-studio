using Amazon.Common.DotNetCli.Tools.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amazon.ECS.Tools
{
    /// <summary>
    /// This class defines all the possible options across all the commands. The individual commands will then
    /// references the options that are appropiate.
    /// </summary>
    public static class ECSDefinedCommandOptions
    {

        public static readonly CommandOption ARGUMENT_DOCKER_TAG =
            new CommandOption
            {
                Name = "Docker Image Tag",
                ShortSwitch = "-t",
                Switch = "--tag",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name and optionally a tag in the 'name:tag' format.",
            };
        public static readonly CommandOption ARGUMENT_SKIP_IMAGE_PUSH =
            new CommandOption
            {
                Name = "Skip Image Push",
                ShortSwitch = "-sip",
                Switch = "--skip-image-push",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "Skip building and push an image to Amazon ECR.",
            };
        public static readonly CommandOption ARGUMENT_ECS_TASK_DEFINITION =
            new CommandOption
            {
                Name = "Task Definition Name",
                ShortSwitch = "-td",
                Switch = "--task-definition",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name of the ECS Task Defintion to be created or updated.",
            };
        public static readonly CommandOption ARGUMENT_ECS_CONTAINER =
            new CommandOption
            {
                Name = "Container Name",
                ShortSwitch = "-cont",
                Switch = "--container",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name of the Container in a Task Definition to be created/updated.",
            };

        public static readonly CommandOption ARGUMENT_ECS_MEMORY_HARD_LIMIT =
            new CommandOption
            {
                Name = "Container Memory Hard Limit",
                ShortSwitch = "-cmhl",
                Switch = "--container-memory-hard-limit",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The hard limit (in MiB) of memory to present to the container.",
            };

        public static readonly CommandOption ARGUMENT_ECS_MEMORY_SOFT_LIMIT =
            new CommandOption
            {
                Name = "Container Memory Soft Limit",
                ShortSwitch = "-cmsl",
                Switch = "--container-memory-soft-limit",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The soft limit (in MiB) of memory to reserve for the container.",
            };

        public static readonly CommandOption ARGUMENT_ECS_CLUSTER =
            new CommandOption
            {
                Name = "Cluster Name",
                ShortSwitch = "-ec",
                Switch = "--cluster",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name of the ECS Cluster to run the docker image.",
            };

        public static readonly CommandOption ARGUMENT_LAUNCH_TYPE =
            new CommandOption
            {
                Name = "Launch Type",
                ShortSwitch = "-lt",
                Switch = "--launch-type",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The launch type on which to run tasks. Valid values EC2 | FARGATE.",
            };

        public static readonly CommandOption ARGUMENT_LAUNCH_SUBNETS =
            new CommandOption
            {
                Name = "Launch VPC Subnets",
                ShortSwitch = "-ls",
                Switch = "--launch-subnets",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "Comma delimited list of subnet ids used when launch type is FARGATE",
            };

        public static readonly CommandOption ARGUMENT_LAUNCH_SECURITYGROUPS =
            new CommandOption
            {
                Name = "Launch Type",
                ShortSwitch = "-lsg",
                Switch = "--launch-security-groups",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "Comma delimited list of security group ids used when launch type is FARGATE",
            };

        public static readonly CommandOption ARGUMENT_ECS_SERVICE =
            new CommandOption
            {
                Name = "Service Name",
                ShortSwitch = "-cs",
                Switch = "--cluster-service",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name of the service to run on the ECS Cluster.",
            };

        public static readonly CommandOption ARGUMENT_ECS_DESIRED_COUNT =
            new CommandOption
            {
                Name = "Desired Count",
                ShortSwitch = "-dc",
                Switch = "--desired-count",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The number of instantiations of the task to place and keep running in your service. Default is 1.",
            };

        public static readonly CommandOption ARGUMENT_ECS_TASK_COUNT =
            new CommandOption
            {
                Name = "Task Count",
                ShortSwitch = "-tc",
                Switch = "--task-count",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The number of instantiations of the task to place and keep running in your service. Default is 1.",
            };

        public static readonly CommandOption ARGUMENT_ECS_PLACEMENT_CONSTRAINTS =
            new CommandOption
            {
                Name = "Placement Constraints",
                Switch = "--placement-constraints",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "Placement constraint to use for tasks in service. Format is <type>=<optional expression>,...",
            };

        public static readonly CommandOption ARGUMENT_ECS_PLACEMENT_STRATEGY =
            new CommandOption
            {
                Name = "Placement Strategy",
                Switch = "--placement-strategy",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "Placement strategy to use for tasks in service. Format is <type>=<optional field>,...",
            };

        public static readonly CommandOption ARGUMENT_ECS_CONTAINER_PORT_MAPPING =
            new CommandOption
            {
                Name = "Container Port Mapping",
                ShortSwitch = "-cpm",
                Switch = "--container-port-mapping",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "The mapping of ports. Format is <host-port>:<container-port>,...",
            };
        public static readonly CommandOption ARGUMENT_ENVIRONMENT_VARIABLES =
            new CommandOption
            {
                Name = "Environment Variables",
                ShortSwitch = "-ev",
                Switch = "--environment-variables",
                ValueType = CommandOption.CommandOptionValueType.KeyValuePairs,
                Description = "Environment variables for a container definition. Format is <key1>=<value1>;<key2>=<value2>."
            };
        public static readonly CommandOption ARGUMENT_TASK_DEFINITION_ROLE =
            new CommandOption
            {
                Name = "Task Definition Role",
                ShortSwitch = "-trole",
                Switch = "--task-role",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The IAM role that will provide AWS credentials for the containers in the Task Definition."
            };
        public static readonly CommandOption ARGUMENT_TASK_DEFINITION_EXECUTION_ROLE =
            new CommandOption
            {
                Name = "Task Definition Execution Role",
                Switch = "--task-execution-role",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The IAM role ECS assumes to pull images from ECR and publish logs to CloudWatch Logs. Fargate only."
            };
        public static readonly CommandOption ARGUMENT_TASK_DEFINITION_CPU =
            new CommandOption
            {
                Name = "Task Definition Allocated CPU",
                Switch = "--task-memory",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The amount of cpu to allocate for the task definition. Fargate only."
            };
        public static readonly CommandOption ARGUMENT_TASK_DEFINITION_MEMORY =
            new CommandOption
            {
                Name = "Task Definition Allocated Memory",
                Switch = "--task-cpu",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The amount of memory to allocated for the task definition. Fargate only."
            };
        public static readonly CommandOption ARGUMENT_ELB_TARGET_GROUP_ARN =
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
                ShortSwitch = "-dmp",
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

        public static readonly CommandOption ARGUMENT_SCHEDULED_RULE_NAME =
            new CommandOption
            {
                Name = "Scheduled Rule",
                Switch = "--rule",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The name of the CloudWatch Event Schedule rule."
            };
        public static readonly CommandOption ARGUMENT_SCHEDULED_RULE_TARGET =
            new CommandOption
            {
                Name = "Schedule Rule Target",
                Switch = "--rule-target",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The name of the target that will be assigned to the rule and point to the ECS task definition."
            };
        public static readonly CommandOption ARGUMENT_SCHEDULE_EXPRESSION =
            new CommandOption
            {
                Name = "Schedule Expression",
                Switch = "--schedule-expression",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The scheduling expression. For example, \"cron(0 20 * * ? *)\" or \"rate(5 minutes)\"."
            };
        public static readonly CommandOption ARGUMENT_CLOUDWATCHEVENT_ROLE =
            new CommandOption
            {
                Name = "CloudWatch Event IAM Role",
                Switch = "--cloudwatch-event-role",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The role that IAM will assume to invoke the target."
            };
        public static readonly CommandOption ARGUMENT_ECS_TASK_GROUP =
            new CommandOption
            {
                Name = "Task Group",
                Switch = "--task-group",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The task group to associate with the task. The default value is the family name of the task definition."
            };
    }
}