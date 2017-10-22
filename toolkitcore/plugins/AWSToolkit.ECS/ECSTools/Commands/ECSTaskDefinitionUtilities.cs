﻿using Amazon.ECS.Model;
using Amazon.ECS.Tools.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.ECS.Tools.Commands
{
    public class ECSTaskDefinitionUtilities
    {
        public static async Task<string> CreateOrUpdateTaskDefinition(IToolLogger logger, IAmazonECS ecsClient, BaseCommand command, 
            TaskDefinitionProperties properties, string dockerImageTag)
        {
            var ecsContainer = command.GetStringValueOrDefault(properties.ECSContainer, DefinedCommandOptions.ARGUMENT_ECS_CONTAINER, true);
            var ecsTaskDefinition = command.GetStringValueOrDefault(properties.ECSTaskDefinition, DefinedCommandOptions.ARGUMENT_ECS_TASK_DEFINITION, true);

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
                    registerRequest.NetworkMode = response.TaskDefinition.NetworkMode;
                    registerRequest.PlacementConstraints = response.TaskDefinition.PlacementConstraints;
                    registerRequest.TaskRoleArn = response.TaskDefinition.TaskRoleArn;
                    registerRequest.Volumes = response.TaskDefinition.Volumes;
                }

                var taskIAMRole = command.GetStringValueOrDefault(properties.TaskDefinitionRole, DefinedCommandOptions.ARGUMENT_TASK_DEFINITION_ROLE, false);
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
                    var environmentVariables = command.GetKeyValuePairOrDefault(properties.EnvironmentVariables, DefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES, false);
                    if (environmentVariables != null && environmentVariables.Count > 0)
                    {
                        var listEnv = new List<KeyValuePair>();
                        foreach (var e in environmentVariables)
                        {
                            listEnv.Add(new KeyValuePair { Name = e.Key, Value = e.Value });
                        }
                        containerDefinition.Environment = listEnv;
                    }
                }
                {
                    var hardLimit = command.GetIntValueOrDefault(properties.ContainerMemoryHardLimit, DefinedCommandOptions.ARGUMENT_ECS_MEMORY_HARD_LIMIT, false);
                    if (hardLimit.HasValue)
                    {
                        logger?.WriteLine($"Setting container hard memory limit {hardLimit.Value}MiB");
                        containerDefinition.Memory = hardLimit.Value;
                    }
                }
                {
                    var softLimit = command.GetIntValueOrDefault(properties.ContainerMemorySoftLimit, DefinedCommandOptions.ARGUMENT_ECS_MEMORY_SOFT_LIMIT, false);
                    if (softLimit.HasValue)
                    {
                        logger?.WriteLine($"Setting container soft memory limit {softLimit.Value}MiB");
                        containerDefinition.MemoryReservation = softLimit.Value;
                    }
                }
                {
                    var portMappings = command.GetStringValuesOrDefault(properties.PortMappings, DefinedCommandOptions.ARGUMENT_ECS_CONTAINER_PORT_MAPPING, false);
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

                            logger?.WriteLine($"Adding port mapping host {tokens[0]} to container {tokens[1]}");
                            containerDefinition.PortMappings.Add(new PortMapping
                            {
                                HostPort = int.Parse(tokens[0]),
                                ContainerPort = int.Parse(tokens[1])
                            });
                        }
                    }
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
                throw new DockerToolsException($"Error updating ECS task defintion {ecsTaskDefinition}: {e.Message}", DockerToolsException.ErrorCode.FailedToUpdateTaskDefinition);
            }
        }
    }
}
