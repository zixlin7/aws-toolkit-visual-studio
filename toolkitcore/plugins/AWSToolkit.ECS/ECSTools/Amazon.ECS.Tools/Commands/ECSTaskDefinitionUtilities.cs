using Amazon.Common.DotNetCli.Tools;
using Amazon.ECS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.ECS.Tools.Commands
{
    public class ECSTaskDefinitionUtilities
    {
        public static async Task<string> CreateOrUpdateTaskDefinition(IToolLogger logger, IAmazonECS ecsClient, ECSBaseCommand command, 
            TaskDefinitionProperties properties, string dockerImageTag, bool isFargate)
        {
            var ecsContainer = command.GetStringValueOrDefault(properties.ECSContainer, ECSDefinedCommandOptions.ARGUMENT_ECS_CONTAINER, true);
            var ecsTaskDefinition = command.GetStringValueOrDefault(properties.ECSTaskDefinition, ECSDefinedCommandOptions.ARGUMENT_ECS_TASK_DEFINITION, true);

            try
            {
                DescribeTaskDefinitionResponse response = null;
                try
                {
                    response = await ecsClient.DescribeTaskDefinitionAsync(new DescribeTaskDefinitionRequest
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
                    logger?.WriteLine("Creating new task definition");
                }
                else
                {
                    logger?.WriteLine("Updating existing task definition");

                    registerRequest.ContainerDefinitions = response.TaskDefinition.ContainerDefinitions;
                    registerRequest.Cpu = response.TaskDefinition.Cpu;
                    registerRequest.ExecutionRoleArn = response.TaskDefinition.ExecutionRoleArn;
                    registerRequest.Memory = response.TaskDefinition.Memory;
                    registerRequest.NetworkMode = response.TaskDefinition.NetworkMode;
                    registerRequest.PlacementConstraints = response.TaskDefinition.PlacementConstraints;
                    registerRequest.RequiresCompatibilities = response.TaskDefinition.RequiresCompatibilities;
                    registerRequest.TaskRoleArn = response.TaskDefinition.TaskRoleArn;
                    registerRequest.Volumes = response.TaskDefinition.Volumes;
                }

                var taskIAMRole = command.GetStringValueOrDefault(properties.TaskDefinitionRole, ECSDefinedCommandOptions.ARGUMENT_TASK_DEFINITION_ROLE, false);
                if (!string.IsNullOrWhiteSpace(taskIAMRole))
                {
                    registerRequest.TaskRoleArn = taskIAMRole;
                }

                var containerDefinition = registerRequest.ContainerDefinitions.FirstOrDefault(x => string.Equals(x.Name, ecsContainer, StringComparison.Ordinal));

                if (containerDefinition == null)
                {
                    logger?.WriteLine("Creating new container definition");

                    containerDefinition = new ContainerDefinition
                    {
                        Name = ecsContainer
                    };
                    registerRequest.ContainerDefinitions.Add(containerDefinition);
                }

                containerDefinition.Image = dockerImageTag;

                {
                    var environmentVariables = command.GetKeyValuePairOrDefault(properties.EnvironmentVariables, ECSDefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES, false);
                    if (environmentVariables != null && environmentVariables.Count > 0)
                    {
                        var listEnv = new List<Amazon.ECS.Model.KeyValuePair>();
                        foreach (var e in environmentVariables)
                        {
                            listEnv.Add(new Amazon.ECS.Model.KeyValuePair { Name = e.Key, Value = e.Value });
                        }
                        containerDefinition.Environment = listEnv;
                    }
                }
                {
                    var hardLimit = command.GetIntValueOrDefault(properties.ContainerMemoryHardLimit, ECSDefinedCommandOptions.ARGUMENT_ECS_MEMORY_HARD_LIMIT, false);
                    if (hardLimit.HasValue)
                    {
                        logger?.WriteLine($"Setting container hard memory limit {hardLimit.Value}MiB");
                        containerDefinition.Memory = hardLimit.Value;
                    }
                }
                {
                    var softLimit = command.GetIntValueOrDefault(properties.ContainerMemorySoftLimit, ECSDefinedCommandOptions.ARGUMENT_ECS_MEMORY_SOFT_LIMIT, false);
                    if (softLimit.HasValue)
                    {
                        logger?.WriteLine($"Setting container soft memory limit {softLimit.Value}MiB");
                        containerDefinition.MemoryReservation = softLimit.Value;
                    }
                }
                {
                    var portMappings = command.GetStringValuesOrDefault(properties.PortMappings, ECSDefinedCommandOptions.ARGUMENT_ECS_CONTAINER_PORT_MAPPING, false);
                    if (portMappings != null)
                    {
                        containerDefinition.PortMappings = new List<PortMapping>();
                        foreach (var mapping in portMappings)
                        {
                            var tokens = mapping.Split(':');
                            if (tokens.Length != 2)
                            {
                                throw new DockerToolsException($"Port mapping {mapping} is invalid. Format should be <host-port>:<container-port>,<host-port>:<container-port>,...", DockerToolsException.CommonErrorCode.CommandLineParseError);
                            }

                            logger?.WriteLine($"Adding port mapping host {tokens[0]} to container {tokens[1]}");
                            containerDefinition.PortMappings.Add(new PortMapping
                            {
                                HostPort = int.Parse(tokens[0]),
                                ContainerPort = int.Parse(tokens[1])
                            });
                        }
                    }
                }

                if(isFargate)
                {
                    registerRequest.NetworkMode = NetworkMode.Awsvpc;

                    if (registerRequest.RequiresCompatibilities == null)
                        registerRequest.RequiresCompatibilities = new List<string>();

                    if (!registerRequest.RequiresCompatibilities.Contains("FARGATE"))
                        registerRequest.RequiresCompatibilities.Add("FARGATE");

                    registerRequest.Memory = command.GetStringValueOrDefault(properties.TaskMemory, ECSDefinedCommandOptions.ARGUMENT_TASK_DEFINITION_MEMORY, true);
                    registerRequest.Cpu = command.GetStringValueOrDefault(properties.TaskCPU, ECSDefinedCommandOptions.ARGUMENT_TASK_DEFINITION_CPU, true);

                    registerRequest.ExecutionRoleArn = command.GetStringValueOrDefault(properties.TaskDefinitionExecutionRole, ECSDefinedCommandOptions.ARGUMENT_TASK_DEFINITION_EXECUTION_ROLE, true);
                }

                var registerResponse = await ecsClient.RegisterTaskDefinitionAsync(registerRequest);
                logger?.WriteLine($"Registered new task definition revision {registerResponse.TaskDefinition.Revision}");
                return registerResponse.TaskDefinition.TaskDefinitionArn;
            }
            catch (DockerToolsException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DockerToolsException($"Error updating ECS task definition {ecsTaskDefinition}: {e.Message}", DockerToolsException.ECSErrorCode.FailedToUpdateTaskDefinition);
            }
        }
    }
}
