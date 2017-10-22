using Amazon.ECS.Tools.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.ECS;
using Amazon.ECS.Model;
using ThirdParty.Json.LitJson;
using System.IO;


namespace Amazon.ECS.Tools.Commands
{
    public class DeployScheduledTaskCommand : BaseCommand
    {
        public const string COMMAND_NAME = "deploy-scheduled-task";
        public const string COMMAND_DESCRIPTION = "Execute \"dotnet publish\", \"docker build\" and then push the image to Amazon ECR.";

        public static readonly IList<CommandOption> CommandOptions = BuildLineOptions(new List<CommandOption>
        {
            DefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            DefinedCommandOptions.ARGUMENT_CONFIGURATION,
            DefinedCommandOptions.ARGUMENT_FRAMEWORK,
            DefinedCommandOptions.ARGUMENT_DOCKER_TAG,

            DefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH,

            DefinedCommandOptions.ARGUMENT_ECS_TASK_DEFINITION,
            DefinedCommandOptions.ARGUMENT_ECS_CONTAINER,
            DefinedCommandOptions.ARGUMENT_ECS_MEMORY_HARD_LIMIT,
            DefinedCommandOptions.ARGUMENT_ECS_MEMORY_SOFT_LIMIT,
            DefinedCommandOptions.ARGUMENT_ECS_CONTAINER_PORT_MAPPING,
            DefinedCommandOptions.ARGUMENT_TASK_DEFINITION_ROLE,
            DefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES,

            DefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_NAME,
            DefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_TARGET,
            DefinedCommandOptions.ARGUMENT_SCHEDULE_EXPRESSION,
            DefinedCommandOptions.ARGUMENT_CLOUDWATCHEVENT_ROLE,

            DefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE,
        });

        PushDockerImageProperties _pushProperties;
        public PushDockerImageProperties PushDockerImageProperties
        {
            get
            {
                if (this._pushProperties == null)
                {
                    this._pushProperties = new PushDockerImageProperties();
                }

                return this._pushProperties;
            }
            set { this._pushProperties = value; }
        }

        DeployScheduledTaskProperties _deployScheduledTaskProperties;
        public DeployScheduledTaskProperties DeployScheduledTaskProperties
        {
            get
            {
                if (this._deployScheduledTaskProperties == null)
                {
                    this._deployScheduledTaskProperties = new DeployScheduledTaskProperties();
                }

                return this._deployScheduledTaskProperties;
            }
            set { this._deployScheduledTaskProperties = value; }
        }

        TaskDefinitionProperties _taskDefinitionProperties;
        public TaskDefinitionProperties TaskDefinitionProperties
        {
            get
            {
                if (this._taskDefinitionProperties == null)
                {
                    this._taskDefinitionProperties = new TaskDefinitionProperties();
                }

                return this._taskDefinitionProperties;
            }
            set { this._taskDefinitionProperties = value; }
        }

        ClusterProperties _clusterProperties;
        public ClusterProperties ClusterProperties
        {
            get
            {
                if (this._clusterProperties == null)
                {
                    this._clusterProperties = new ClusterProperties();
                }

                return this._clusterProperties;
            }
            set { this._clusterProperties = value; }
        }

        public bool? PersistConfigFile { get; set; }

        public DeployScheduledTaskCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, CommandOptions, args)
        {
        }

        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            this.PushDockerImageProperties.ParseCommandArguments(values);
            this.TaskDefinitionProperties.ParseCommandArguments(values);
            this.ClusterProperties.ParseCommandArguments(values);
            this.DeployScheduledTaskProperties.ParseCommandArguments(values);

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE.Switch)) != null)
                this.PersistConfigFile = tuple.Item2.BoolValue;
        }

        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                var skipPush = this.GetBoolValueOrDefault(this.DeployScheduledTaskProperties.SkipImagePush, DefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH, false).GetValueOrDefault();
                var ecsContainer = this.GetStringValueOrDefault(this.TaskDefinitionProperties.ECSContainer, DefinedCommandOptions.ARGUMENT_ECS_CONTAINER, true);
                var ecsTaskDefinition = this.GetStringValueOrDefault(this.TaskDefinitionProperties.ECSTaskDefinition, DefinedCommandOptions.ARGUMENT_ECS_TASK_DEFINITION, true);


                string dockerImageTag = this.GetStringValueOrDefault(this.PushDockerImageProperties.DockerImageTag, DefinedCommandOptions.ARGUMENT_DOCKER_TAG, true);

                if (!dockerImageTag.Contains(":"))
                    dockerImageTag += ":latest";

                if (skipPush)
                {
                    dockerImageTag = await Utilities.ExpandImageTagIfNecessary(this.Logger, this.ECRClient, dockerImageTag);
                }
                else
                {
                    var pushCommand = new PushDockerImageCommand(this.Logger, this.WorkingDirectory, this.OriginalCommandLineArguments)
                    {
                        ConfigFile = this.ConfigFile,
                        DisableInteractive = this.DisableInteractive,
                        Credentials = this.Credentials,
                        ECRClient = this.ECRClient,
                        Profile = this.Profile,
                        ProfileLocation = this.ProfileLocation,
                        ProjectLocation = this.ProjectLocation,
                        Region = this.Region,
                        WorkingDirectory = this.WorkingDirectory,

                        PushDockerImageProperties = this.PushDockerImageProperties,
                    };
                    var success = await pushCommand.ExecuteAsync();

                    if (!success)
                        return false;

                    dockerImageTag = pushCommand.PushedImageUri;
                }

                var taskDefinitionArn = await ECSTaskDefinitionUtilities.CreateOrUpdateTaskDefinition(this.Logger, this.ECSClient,
                    this, this.TaskDefinitionProperties, dockerImageTag);

                var ecsCluster = this.GetStringValueOrDefault(this.ClusterProperties.ECSCluster, DefinedCommandOptions.ARGUMENT_ECS_CLUSTER, true);

                if(!ecsCluster.Contains(":"))
                {
                    var arnPrefix = taskDefinitionArn.Substring(0, taskDefinitionArn.LastIndexOf(":task"));
                    ecsCluster = arnPrefix + ":cluster/" + ecsCluster;
                }

                var ruleName = this.GetStringValueOrDefault(this.DeployScheduledTaskProperties.ScheduleTaskRule, DefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_NAME, true);
                var targetName = this.GetStringValueOrDefault(this.DeployScheduledTaskProperties.ScheduleTaskRule, DefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_NAME, false);
                if (string.IsNullOrEmpty(targetName))
                    targetName = ruleName;

                var scheduleExpression = this.GetStringValueOrDefault(this.DeployScheduledTaskProperties.ScheduleExpression, DefinedCommandOptions.ARGUMENT_SCHEDULE_EXPRESSION, true);
                var cweRole = this.GetStringValueOrDefault(this.DeployScheduledTaskProperties.CloudWatchEventIAMRole, DefinedCommandOptions.ARGUMENT_CLOUDWATCHEVENT_ROLE, true);
                var desiredCount = this.GetIntValueOrDefault(this.DeployScheduledTaskProperties.DesiredCount, DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT, false);
                if (!desiredCount.HasValue)
                    desiredCount = 1;

                string ruleArn = null;
                try
                {
                    ruleArn = this.CWEClient.PutRule(new PutRuleRequest
                    {
                        Name = ruleName,
                        ScheduleExpression = scheduleExpression,
                        State = RuleState.ENABLED
                    }).RuleArn;

                    this.Logger?.WriteLine($"Put CloudWatch Event rule {ruleName} with expression {scheduleExpression}");
                }
                catch(Exception e)
                {
                    throw new DockerToolsException("Error creating CloudWatch Event rule: " + e.Message, DockerToolsException.ErrorCode.PutRuleFail);
                }

                try
                {
                    this.CWEClient.PutTargets(new PutTargetsRequest
                    {
                        Rule = ruleName,
                        Targets = new List<Target>
                        {
                            new Target
                            {
                                Arn = ecsCluster,
                                RoleArn = cweRole,
                                Id = targetName,
                                EcsParameters = new EcsParameters
                                {
                                    TaskCount = desiredCount.Value,
                                    TaskDefinitionArn = taskDefinitionArn
                                }
                            }
                        }
                    });
                    this.Logger?.WriteLine($"Put CloudWatch Event target {targetName}");
                }
                catch (Exception e)
                {
                    throw new DockerToolsException("Error creating CloudWatch Event target: " + e.Message, DockerToolsException.ErrorCode.PutTargetFail);
                }

                if (this.GetBoolValueOrDefault(this.PersistConfigFile, DefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE, false).GetValueOrDefault())
                {
                    this.SaveConfigFile();
                }
            }
            catch (DockerToolsException e)
            {
                this.Logger.WriteLine(e.Message);
                this.LastToolsException = e;
                return false;
            }
            catch (Exception e)
            {
                this.Logger.WriteLine($"Unknown error executing docker push to Amazon EC2 Container Registry: {e.Message}");
                this.Logger.WriteLine(e.StackTrace);
                return false;
            }
            return true;
        }

        protected override void SaveConfigFile(JsonData data)
        {
            this.PushDockerImageProperties.PersistSettings(this, data);
            this.TaskDefinitionProperties.PersistSettings(this, data);
            this.ClusterProperties.PersistSettings(this, data);
            this.DeployScheduledTaskProperties.PersistSettings(this, data);
        }
    }
}
