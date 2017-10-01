using Amazon.ECS.Tools.Options;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Task = System.Threading.Tasks.Task;

using Amazon.ECR.Model;
using Amazon.ECS.Model;




namespace Amazon.ECS.Tools.Commands
{
    public class DeployCommand : BaseCommand
    {
        public const string COMMAND_NAME = "deploy";

        public static readonly IList<CommandOption> CommandOptions = new List<CommandOption>
        {
            DefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            DefinedCommandOptions.ARGUMENT_CONFIGURATION,
            DefinedCommandOptions.ARGUMENT_FRAMEWORK,
            DefinedCommandOptions.ARGUMENT_DOCKER_TAG,
            DefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH,
            DefinedCommandOptions.ARGUMENT_ECS_TASK_DEFINITION,
            DefinedCommandOptions.ARGUMENT_ECS_CONTAINER,
            DefinedCommandOptions.ARGUMENT_ECS_CLUSTER,
            DefinedCommandOptions.ARGUMENT_ECS_SERVICE,
            DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT,
            DefinedCommandOptions.ARGUMENT_ECS_CONTAINER_PORT_MAPPING,
        };

        public string Configuration { get; set; }
        public string TargetFramework { get; set; }
        public string DockerImageTag { get; set; }

        public bool SkipImagePush { get; set; }
        public string ECSTaskDefinition { get; set; }
        public string ECSContainer { get; set; }
        public string ECSCluster { get; set; }
        public string ECSService { get; set; }

        public string[] PortMappings { get; set; }

        public int? ContainerMemoryHardLimit { get; set; }
        public int? ContainerMemorySoftLimit { get; set; }
        public int? DesiredCount { get; set; }



        public DeployCommand(IToolLogger logger, string workingDirectory, string[] args)
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

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_CONFIGURATION.Switch)) != null)
                this.Configuration = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_FRAMEWORK.Switch)) != null)
                this.TargetFramework = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH.Switch)) != null)
                this.SkipImagePush = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_TASK_DEFINITION.Switch)) != null)
                this.ECSTaskDefinition = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_CONTAINER.Switch)) != null)
                this.ECSContainer = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_MEMORY_HARD_LIMIT.Switch)) != null)
                this.ContainerMemoryHardLimit = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_MEMORY_SOFT_LIMIT.Switch)) != null)
                this.ContainerMemorySoftLimit = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_CLUSTER.Switch)) != null)
                this.ECSCluster = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_SERVICE.Switch)) != null)
                this.ECSService = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT.Switch)) != null)
                this.DesiredCount = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_ECS_CONTAINER_PORT_MAPPING.Switch)) != null)
                this.PortMappings = tuple.Item2.StringValues;
        }

        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                var skipPush = this.GetBoolValueOrDefault(this.SkipImagePush, DefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH, false).GetValueOrDefault();
                var ecsContainer = this.GetStringValueOrDefault(this.ECSContainer, DefinedCommandOptions.ARGUMENT_ECS_CONTAINER, true);
                var ecsTaskDefinition = this.GetStringValueOrDefault(this.ECSTaskDefinition, DefinedCommandOptions.ARGUMENT_ECS_TASK_DEFINITION, true);


                string dockerImageTag = this.GetStringValueOrDefault(this.DockerImageTag, DefinedCommandOptions.ARGUMENT_DOCKER_TAG, true);

                if (!dockerImageTag.Contains(":"))
                    dockerImageTag += ":latest";

                if(skipPush)
                {
                    dockerImageTag = await ExpandImageTagIfNecessary(dockerImageTag);
                }
                else
                {
                    var pushCommand = new PushDockerImageCommand(this.Logger, this.WorkingDirectory, this.OriginalCommandLineArguments)
                    {
                        ConfigFile = this.ConfigFile,
                        Configuration = this.Configuration,
                        DisableInteractive = this.DisableInteractive,
                        Credentials = this.Credentials,
                        DockerImageTag = DockerImageTag,
                        ECRClient = this.ECRClient,
                        Profile = this.Profile,
                        ProfileLocation = this.ProfileLocation,
                        ProjectLocation = this.ProjectLocation,
                        Region = this.Region,
                        TargetFramework = this.TargetFramework,
                        WorkingDirectory = this.WorkingDirectory
                    };
                    await pushCommand.ExecuteAsync();
                    dockerImageTag = pushCommand.PushedImageUri;
                }

                int revision = await CreateOrUpdateTaskDefinition(ecsTaskDefinition, dockerImageTag, ecsContainer);

                var ecsCluster = this.GetStringValueOrDefault(this.ECSCluster, DefinedCommandOptions.ARGUMENT_ECS_CLUSTER, true);
                var ecsService = this.GetStringValueOrDefault(this.ECSService, DefinedCommandOptions.ARGUMENT_ECS_SERVICE, true);

                await CreateOrUpdateService(ecsCluster, ecsService, ecsTaskDefinition, revision);
                this.Logger?.WriteLine($"Service {ecsService} on ECS cluster {ecsCluster} has been updated. The Cluster will now deploy the new service version.");
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

        private async Task<string> ExpandImageTagIfNecessary(string dockerImageTag)
        {
            try
            {
                if (dockerImageTag.Contains(".amazonaws."))
                    return dockerImageTag;

                string repositoryName = dockerImageTag;
                if (repositoryName.Contains(":"))
                    repositoryName = repositoryName.Substring(0, repositoryName.IndexOf(':'));

                DescribeRepositoriesResponse describeResponse = null;
                try
                {
                    describeResponse = await this.ECRClient.DescribeRepositoriesAsync(new DescribeRepositoriesRequest
                    {
                        RepositoryNames = new List<string> { repositoryName }
                    });
                }
                catch (Exception e)
                {
                    if (!(e is RepositoryNotFoundException))
                    {
                        throw;
                    }
                }

                // Not found in ECR, assume pulling Docker Hub
                if (describeResponse == null)
                {
                    return dockerImageTag;
                }

                var fullPath = describeResponse.Repositories[0].RepositoryUri + dockerImageTag.Substring(dockerImageTag.IndexOf(':'));
                this.Logger?.WriteLine($"Determine full image name to be {fullPath}");
                return fullPath;
            }
            catch (DockerToolsException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DockerToolsException($"Error determing full repository path for the image {dockerImageTag}: {e.Message}", DockerToolsException.ErrorCode.FailedToExpandImageTag);
            }
        }

        private async Task<int> CreateOrUpdateTaskDefinition(string ecsTaskDefinition, string dockerImageTag, string ecsContainer)
        {
            try
            {
                DescribeTaskDefinitionResponse response = null;
                try
                {
                    response = await this.ECSClient.DescribeTaskDefinitionAsync(new DescribeTaskDefinitionRequest
                    {
                        TaskDefinition = ecsTaskDefinition
                    });
                }
                catch (Exception e)
                {
                    if (!(e is ClientException))
                    {
                        throw;
                    }
                }

                var registerRequest = new RegisterTaskDefinitionRequest()
                {
                    Family = ecsTaskDefinition
                };

                if (response == null || response.TaskDefinition == null)
                {
                    this.Logger?.WriteLine("Creating new task definition");
                }
                else
                {
                    this.Logger?.WriteLine("Updating existing task definition");

                    registerRequest.ContainerDefinitions = response.TaskDefinition.ContainerDefinitions;
                    registerRequest.NetworkMode = response.TaskDefinition.NetworkMode;
                    registerRequest.PlacementConstraints = response.TaskDefinition.PlacementConstraints;
                    registerRequest.TaskRoleArn = response.TaskDefinition.TaskRoleArn;
                    registerRequest.Volumes = response.TaskDefinition.Volumes;
                }

                var containerDefinition = registerRequest.ContainerDefinitions.FirstOrDefault(x => string.Equals(x.Name, ecsContainer, StringComparison.Ordinal));

                if (containerDefinition == null)
                {
                    this.Logger?.WriteLine("Creating new container definition");

                    containerDefinition = new ContainerDefinition
                    {
                        Name = ecsContainer
                    };
                    registerRequest.ContainerDefinitions.Add(containerDefinition);
                }

                containerDefinition.Image = dockerImageTag;

                {
                    var hardLimit = this.GetIntValueOrDefault(this.ContainerMemoryHardLimit, DefinedCommandOptions.ARGUMENT_ECS_MEMORY_HARD_LIMIT, false);
                    if (hardLimit.HasValue)
                    {
                        this.Logger?.WriteLine($"Setting container hard memory limit {hardLimit.Value}MiB");
                        containerDefinition.Memory = hardLimit.Value;
                    }
                }
                {
                    var softLimit = this.GetIntValueOrDefault(this.ContainerMemorySoftLimit, DefinedCommandOptions.ARGUMENT_ECS_MEMORY_SOFT_LIMIT, false);
                    if (softLimit.HasValue)
                    {
                        this.Logger?.WriteLine($"Setting container soft memory limit {softLimit.Value}MiB");
                        containerDefinition.MemoryReservation = softLimit.Value;
                    }
                }
                {
                    var portMappings = this.GetStringValuesOrDefault(this.PortMappings, DefinedCommandOptions.ARGUMENT_ECS_CONTAINER_PORT_MAPPING, false);
                    if (portMappings != null)
                    {
                        containerDefinition.PortMappings = new List<PortMapping>();
                        foreach (var mapping in portMappings)
                        {
                            var tokens = mapping.Split(':');
                            if (tokens.Length != 2)
                            {
                                throw new DockerToolsException($"Port mapping {mapping} is invalid. Format should be <host-port>:<container-port>,<host-port>:<container-port>,...", DockerToolsException.ErrorCode.CommandLineParseError);
                            }

                            this.Logger?.WriteLine($"Adding port mapping host {tokens[0]} to container {tokens[1]}");
                            containerDefinition.PortMappings.Add(new PortMapping
                            {
                                HostPort = int.Parse(tokens[0]),
                                ContainerPort = int.Parse(tokens[1])
                            });
                        }
                    }
                }


                var registerResponse = await this.ECSClient.RegisterTaskDefinitionAsync(registerRequest);
                this.Logger?.WriteLine($"Registered new task definition revision {registerResponse.TaskDefinition.Revision}");
                return registerResponse.TaskDefinition.Revision;
            }
            catch(DockerToolsException)
            {
                throw;
            }
            catch(Exception e)
            {
                throw new DockerToolsException($"Error updating ECS task defintion {ecsTaskDefinition}: {e.Message}", DockerToolsException.ErrorCode.FailedToUpdateTaskDefinition);
            }
        }

        private async Task CreateOrUpdateService(string ecsCluster, string ecsService, string taskDefinition, int taskDefinitionRevision)
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

                var desiredCount = this.GetIntValueOrDefault(this.ContainerMemoryHardLimit, DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT, false);

                if (describeServiceResponse.Services.Count == 0 || describeServiceResponse.Services[0].Status == "INACTIVE")
                {
                    this.Logger?.WriteLine($"Creating new service: {ecsService}");
                    await this.ECSClient.CreateServiceAsync(new CreateServiceRequest
                    {
                        ClientToken = Guid.NewGuid().ToString(),
                        Cluster = ecsCluster,
                        ServiceName = ecsService,
                        TaskDefinition = $"{taskDefinition}:{taskDefinitionRevision}",
                        DesiredCount = desiredCount.HasValue ? desiredCount.Value : 1
                    });
                }
                else
                {
                    this.Logger?.WriteLine($"Updating new service: {ecsService}");
                    var updateRequest = new UpdateServiceRequest
                    {
                        Cluster = ecsCluster,
                        Service = ecsService,
                        TaskDefinition = $"{taskDefinition}:{taskDefinitionRevision}",
                        DeploymentConfiguration = new DeploymentConfiguration
                        {
                            MinimumHealthyPercent = 0
                        }
                    };

                    if (describeClusterResponse.Clusters[0].RegisteredContainerInstancesCount == 1)
                    {
                        this.Logger?.WriteLine("Allowing minimum health to go down to zero during deployment since there is only one EC2 instance in the ECS cluster.");
                        updateRequest.DeploymentConfiguration = new DeploymentConfiguration
                        {
                            MinimumHealthyPercent = 0
                        };
                    }

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

    }
}
