using Amazon.ECS.Tools.Options;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Task = System.Threading.Tasks.Task;

using Amazon.ECR.Model;
using Amazon.ECS.Model;
using ThirdParty.Json.LitJson;
using System.IO;

namespace Amazon.ECS.Tools.Commands
{
    public class DeployServiceCommand : BaseCommand
    {
        public const string COMMAND_NAME = "deploy-service";
        public const string COMMAND_DESCRIPTION = "Deploy the application to an Amazon ECS Cluster.";

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

            DefinedCommandOptions.ARGUMENT_ECS_CLUSTER,
            DefinedCommandOptions.ARGUMENT_ECS_SERVICE,
            DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT,
            DefinedCommandOptions.ARGUMENT_DEPLOYMENT_MAXIMUM_PERCENT,
            DefinedCommandOptions.ARGUMENT_DEPLOYMENT_MINIMUM_HEALTHY_PERCENT,

            DefinedCommandOptions.ARGUMENT_ELB_SERVICE_ROLE,
            DefinedCommandOptions.ARGUMENT_ELB_TARGET_GROUP_ARN,
            DefinedCommandOptions.ARGUMENT_ELB_CONTAINER_PORT,

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

        DeployServiceProperties _deployServiceProperties;
        public DeployServiceProperties DeployServiceProperties
        {
            get
            {
                if (this._deployServiceProperties == null)
                {
                    this._deployServiceProperties = new DeployServiceProperties();
                }

                return this._deployServiceProperties;
            }
            set { this._deployServiceProperties = value; }
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


        public bool? PersistConfigFile { get; set; }

        public DeployServiceCommand(IToolLogger logger, string workingDirectory, string[] args)
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
            this.DeployServiceProperties.ParseCommandArguments(values);

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE.Switch)) != null)
                this.PersistConfigFile = tuple.Item2.BoolValue;

        }

        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                var skipPush = this.GetBoolValueOrDefault(this.DeployServiceProperties.SkipImagePush, DefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH, false).GetValueOrDefault();
                var ecsContainer = this.GetStringValueOrDefault(this.TaskDefinitionProperties.ECSContainer, DefinedCommandOptions.ARGUMENT_ECS_CONTAINER, true);
                var ecsTaskDefinition = this.GetStringValueOrDefault(this.TaskDefinitionProperties.ECSTaskDefinition, DefinedCommandOptions.ARGUMENT_ECS_TASK_DEFINITION, true);


                string dockerImageTag = this.GetStringValueOrDefault(this.PushDockerImageProperties.DockerImageTag, DefinedCommandOptions.ARGUMENT_DOCKER_TAG, true);

                if (!dockerImageTag.Contains(":"))
                    dockerImageTag += ":latest";

                if(skipPush)
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
                var ecsService = this.GetStringValueOrDefault(this.DeployServiceProperties.ECSService, DefinedCommandOptions.ARGUMENT_ECS_SERVICE, true);

                await CreateOrUpdateService(ecsCluster, ecsService, taskDefinitionArn, ecsContainer);
                this.Logger?.WriteLine($"Service {ecsService} on ECS cluster {ecsCluster} has been updated. The Cluster will now deploy the new service version.");

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
                this.Logger.WriteLine($"Unknown error executing deploy application to an ECS service: {e.Message}");
                this.Logger.WriteLine(e.StackTrace);
                return false;
            }
            return true;
        }



        private async Task CreateOrUpdateService(string ecsCluster, string ecsService, string taskDefinitionArn, string ecsContainer)
        {
            try
            { 
                var describeClusterResponse = await this.ECSClient.DescribeClustersAsync(new DescribeClustersRequest
                {
                    Clusters = new List<string> { ecsCluster }
                });

                if(describeClusterResponse.Clusters.Count == 0)
                {
                    throw new DockerToolsException($"Cluster {ecsCluster} can not be found.", DockerToolsException.ErrorCode.ClusterNotFound);
                }

                var describeServiceResponse = await this.ECSClient.DescribeServicesAsync(new DescribeServicesRequest
                {
                    Cluster = ecsCluster,
                    Services = new List<string> { ecsService }
                });

                var desiredCount = this.GetIntValueOrDefault(this.DeployServiceProperties.DesiredCount, DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT, false);
                var deploymentMaximumPercent = this.GetIntValueOrDefault(this.DeployServiceProperties.DeploymentMaximumPercent, DefinedCommandOptions.ARGUMENT_DEPLOYMENT_MAXIMUM_PERCENT, false);
                var deploymentMinimumHealthyPercent = this.GetIntValueOrDefault(this.DeployServiceProperties.DeploymentMinimumHealthyPercent, DefinedCommandOptions.ARGUMENT_DEPLOYMENT_MINIMUM_HEALTHY_PERCENT, false);

                DeploymentConfiguration deploymentConfiguration = null;
                if (deploymentMaximumPercent.HasValue || deploymentMinimumHealthyPercent.HasValue)
                {
                    deploymentConfiguration = new DeploymentConfiguration();
                    if (deploymentMaximumPercent.HasValue)
                        deploymentConfiguration.MaximumPercent = deploymentMaximumPercent.Value;
                    if (deploymentMinimumHealthyPercent.HasValue)
                        deploymentConfiguration.MinimumHealthyPercent = deploymentMinimumHealthyPercent.Value;
                }

                if (describeServiceResponse.Services.Count == 0 || describeServiceResponse.Services[0].Status == "INACTIVE")
                {
                    this.Logger?.WriteLine($"Creating new service: {ecsService}");
                    var request = new CreateServiceRequest
                    {
                        ClientToken = Guid.NewGuid().ToString(),
                        Cluster = ecsCluster,
                        ServiceName = ecsService,
                        TaskDefinition = taskDefinitionArn,
                        DesiredCount = desiredCount.HasValue ? desiredCount.Value : 1,
                        DeploymentConfiguration = deploymentConfiguration
                    };

                    var elbTargetGroup = this.GetStringValueOrDefault(this.DeployServiceProperties.ELBTargetGroup, DefinedCommandOptions.ARGUMENT_ELB_TARGET_GROUP_ARN, false);
                    if(!string.IsNullOrWhiteSpace(elbTargetGroup))
                    {
                        var serviceRole = this.GetStringValueOrDefault(this.DeployServiceProperties.ELBServiceRole, DefinedCommandOptions.ARGUMENT_ELB_SERVICE_ROLE, false);
                        var port = this.GetIntValueOrDefault(this.DeployServiceProperties.ELBContainerPort, DefinedCommandOptions.ARGUMENT_ELB_CONTAINER_PORT, false);
                        if (!port.HasValue)
                            port = 80;
                        request.LoadBalancers.Add(new LoadBalancer
                        {
                            TargetGroupArn = elbTargetGroup,
                            ContainerName = ecsContainer,
                            ContainerPort = port.Value
                        });

                        request.Role = serviceRole;
                    }

                    await this.ECSClient.CreateServiceAsync(request);
                }
                else
                {
                    this.Logger?.WriteLine($"Updating new service: {ecsService}");
                    var updateRequest = new UpdateServiceRequest
                    {
                        Cluster = ecsCluster,
                        Service = ecsService,
                        TaskDefinition = taskDefinitionArn,
                        DeploymentConfiguration = deploymentConfiguration
                    };

                    if(desiredCount.HasValue)
                    {
                        updateRequest.DesiredCount = desiredCount.Value;
                    }

                    //if (describeClusterResponse.Clusters[0].RegisteredContainerInstancesCount == 1)
                    //{
                    //    this.Logger?.WriteLine("Allowing minimum health to go down to zero during deployment since there is only one EC2 instance in the ECS cluster.");
                    //    updateRequest.DeploymentConfiguration = new DeploymentConfiguration
                    //    {
                    //        MinimumHealthyPercent = 0
                    //    };
                    //}

                    await this.ECSClient.UpdateServiceAsync(updateRequest);
                }
            }
            catch (DockerToolsException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DockerToolsException($"Error updating ECS service {ecsService} on cluster {ecsCluster}: {e.Message}", DockerToolsException.ErrorCode.FailedToUpdateService);
            }
        }

        protected override void SaveConfigFile(JsonData data)
        {
            PushDockerImageProperties.PersistSettings(this, data);
            ClusterProperties.PersistSettings(this, data);
            TaskDefinitionProperties.PersistSettings(this, data);
            DeployServiceProperties.PersistSettings(this, data);
        }
    }
}
