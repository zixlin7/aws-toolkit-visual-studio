using Amazon.ECS.Tools.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdParty.Json.LitJson;

namespace Amazon.ECS.Tools.Commands
{
    public class DeployTaskCommand : BaseCommand
    {
        public const string COMMAND_NAME = "deploy-scheduled-task";
        public const string COMMAND_DESCRIPTION = "Push the application to ECR and then runs it as a task on the ECS Cluster.";

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

            DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT,
            DefinedCommandOptions.ARGUMENT_ECS_TASK_GROUP,

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

        DeployTaskProperties _deployTaskProperties;
        public DeployTaskProperties DeployTaskProperties
        {
            get
            {
                if (this._deployTaskProperties == null)
                {
                    this._deployTaskProperties = new DeployTaskProperties();
                }

                return this._deployTaskProperties;
            }
            set { this._deployTaskProperties = value; }
        }

        public bool? PersistConfigFile { get; set; }

        public DeployTaskCommand(IToolLogger logger, string workingDirectory, string[] args)
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
            this.DeployTaskProperties.ParseCommandArguments(values);

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE.Switch)) != null)
                this.PersistConfigFile = tuple.Item2.BoolValue;
        }

        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                var skipPush = this.GetBoolValueOrDefault(this.DeployTaskProperties.SkipImagePush, DefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH, false).GetValueOrDefault();
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

                var desiredCount = this.GetIntValueOrDefault(this.DeployTaskProperties.DesiredCount, DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT, false);
                if (!desiredCount.HasValue)
                    desiredCount = 1;

                var taskGroup = this.GetStringValueOrDefault(this.DeployTaskProperties.TaskGroup, DefinedCommandOptions.ARGUMENT_ECS_TASK_GROUP, false);

                var runTaskRequest = new Amazon.ECS.Model.RunTaskRequest
                {
                    Cluster = ecsCluster,
                    TaskDefinition = taskDefinitionArn,
                    Count = desiredCount.Value
                };

                if (!string.IsNullOrEmpty(taskGroup))
                    runTaskRequest.Group = taskGroup;


                try
                {
                    var response = await this.ECSClient.RunTaskAsync(runTaskRequest);
                    this.Logger?.WriteLine($"Started {response.Tasks.Count} task:");
                    foreach(var task in response.Tasks)
                    {
                        this.Logger?.WriteLine($"\t{task.TaskArn}");
                    }
                }
                catch(Exception e)
                {
                    throw new DockerToolsException("Error deploy task: " + e.Message, DockerToolsException.ErrorCode.RunTaskFail);
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
        }

    }
}
