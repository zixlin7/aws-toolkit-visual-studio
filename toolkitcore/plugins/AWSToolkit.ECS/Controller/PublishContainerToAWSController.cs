using System;
using System.Collections.Generic;
using System.IO;
using Amazon.AWSToolkit.Navigator;
using log4net;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.ECS.WizardPages.PageControllers;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.ECS.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Telemetry;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class PublishContainerToAWSController
    {
        ILog LOGGER = LogManager.GetLogger(typeof(PublishContainerToAWSController));
        private readonly ToolkitContext _toolkitContext;

        ActionResults _results;

        public PublishContainerToAWSController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public ActionResults Execute(Dictionary<string, object> seedValues)
        {
            DisplayPublishWizard(seedValues);
            return _results;
        }

        private void LoadPreviousSettings(Dictionary<string, object> seedValues)
        {
            var sourcePath = seedValues[PublishContainerToAWSWizardProperties.SourcePath] as string;
            if (!File.Exists(Path.Combine(sourcePath, ECSToolsDefaults.DEFAULT_FILE_NAME)))
                return;

            try
            {
                var defaults = new ECSToolsDefaults();
                defaults.LoadDefaults(sourcePath, ECSToolsDefaults.DEFAULT_FILE_NAME);

                Action<CommandOption, string> copyValues = (commandOption, seedKey) =>
                {
                    var value = defaults.GetValueAsString(commandOption);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        if (commandOption.ValueType == CommandOption.CommandOptionValueType.BoolValue)
                        {
                            bool b;
                            if (bool.TryParse(value, out b))
                                seedValues[seedKey] = b;
                        }
                        else if (commandOption.ValueType == CommandOption.CommandOptionValueType.IntValue)
                        {
                            int b;
                            if (int.TryParse(value, out b))
                            {
                                if (commandOption == ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_HARD_LIMIT || commandOption == ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_SOFT_LIMIT)
                                {
                                    seedValues[seedKey] = new int?(b);
                                }
                                else
                                {
                                    seedValues[seedKey] = b;
                                }
                            }
                        }
                        else if(commandOption.ValueType == CommandOption.CommandOptionValueType.CommaDelimitedList)
                        {
                            var tokens = value.Split(',');
                            seedValues[seedKey] = tokens;
                        }
                        else
                        {
                            seedValues[seedKey] = defaults.GetValueAsString(commandOption);
                        }
                    }
                };

                if (defaults.GetValueAsString(CommonDefinedCommandOptions.ARGUMENT_DOCKER_TAG) != null)
                {
                    var fullName = defaults.GetValueAsString(CommonDefinedCommandOptions.ARGUMENT_DOCKER_TAG);

                    if (fullName.Contains(":"))
                    {
                        var tokens = fullName.Split(':');
                        seedValues[PublishContainerToAWSWizardProperties.DockerRepository] = tokens[0];
                        seedValues[PublishContainerToAWSWizardProperties.DockerTag] = tokens[1];
                    }
                    else
                    {
                        seedValues[PublishContainerToAWSWizardProperties.DockerRepository] = fullName;
                    }
                }

                copyValues(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, PublishContainerToAWSWizardProperties.Configuration);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY, PublishContainerToAWSWizardProperties.DockerBuildWorkingDirectory);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_TD_NAME, PublishContainerToAWSWizardProperties.TaskDefinition);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_NAME, PublishContainerToAWSWizardProperties.Container);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_HARD_LIMIT, PublishContainerToAWSWizardProperties.MemoryHardLimit);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_SOFT_LIMIT, PublishContainerToAWSWizardProperties.MemorySoftLimit);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_TD_ROLE, PublishContainerToAWSWizardProperties.TaskRole);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_TD_EXECUTION_ROLE, PublishContainerToAWSWizardProperties.TaskExecutionRole);

                copyValues(ECSDefinedCommandOptions.ARGUMENT_ECS_SERVICE, PublishContainerToAWSWizardProperties.Service);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT, PublishContainerToAWSWizardProperties.DesiredCount);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_DEPLOYMENT_MAXIMUM_PERCENT, PublishContainerToAWSWizardProperties.MaximumPercent);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_DEPLOYMENT_MINIMUM_HEALTHY_PERCENT, PublishContainerToAWSWizardProperties.MinimumHealthy);

                copyValues(ECSDefinedCommandOptions.ARGUMENT_ECS_CLUSTER, PublishContainerToAWSWizardProperties.ClusterName);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_TYPE, PublishContainerToAWSWizardProperties.LaunchType);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_TD_CPU, PublishContainerToAWSWizardProperties.AllocatedTaskCPU);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_TD_MEMORY, PublishContainerToAWSWizardProperties.AllocatedTaskMemory);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_TD_PLATFORM_VERSION, PublishContainerToAWSWizardProperties.PlatformVersion);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SUBNETS, PublishContainerToAWSWizardProperties.LaunchSubnets);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SECURITYGROUPS, PublishContainerToAWSWizardProperties.LaunchSecurityGroups);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_ASSIGN_PUBLIC_IP, PublishContainerToAWSWizardProperties.AssignPublicIpAddress);

                copyValues(ECSDefinedCommandOptions.ARGUMENT_ECS_TASK_COUNT, PublishContainerToAWSWizardProperties.DesiredCount);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_ECS_TASK_GROUP, PublishContainerToAWSWizardProperties.TaskGroup);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_SCHEDULE_EXPRESSION, PublishContainerToAWSWizardProperties.ScheduleExpression);

                if (!string.IsNullOrEmpty(defaults.GetRawString(WizardPages.ECSWizardUtils.PERSISTED_DEPLOYMENT_MODE)))
                {
                    Constants.DeployMode mode;
                    if(Enum.TryParse<Constants.DeployMode>(defaults.GetRawString(WizardPages.ECSWizardUtils.PERSISTED_DEPLOYMENT_MODE), out mode))
                    {
                        seedValues[PublishContainerToAWSWizardProperties.DeploymentMode] = mode;
                    }
                }
                

                if (defaults.GetValueAsString(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_PORT_MAPPING) != null)
                {
                    var mappings = new List<WizardPages.PageUI.PortMappingItem>();
                    var portMappingStr = defaults.GetValueAsString(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_PORT_MAPPING);
                    foreach (var token in portMappingStr.Split(';'))
                    {
                        var ports = token.Split(':');
                        if (ports.Length == 2)
                        {
                            mappings.Add(new PortMappingItem
                            {
                                HostPort = int.Parse(ports[0]),
                                ContainerPort = int.Parse(ports[1])
                            });
                        }
                    }

                    seedValues[PublishContainerToAWSWizardProperties.PortMappings] = mappings;
                }

                if (defaults.GetValueAsString(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ENVIRONMENT_VARIABLES) != null)
                {
                    var items = new List<WizardPages.PageUI.EnvironmentVariableItem>();
                    var str = defaults.GetValueAsString(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ENVIRONMENT_VARIABLES);
                    foreach (var token in str.Split(';'))
                    {
                        var tokens = token.Split('=');
                        if (tokens.Length == 2)
                        {
                            items.Add(new EnvironmentVariableItem
                            {
                                Variable = tokens[0].Replace("\"", ""),
                                Value = tokens[1].Replace("\"", "")
                            });
                        }
                    }

                    if (items.Count > 0)
                    {
                        seedValues[PublishContainerToAWSWizardProperties.EnvironmentVariables] = items;
                    }
                }
            }
            catch(Exception e)
            {
                LOGGER.Error("Error parsing existing file " + sourcePath, e);
            }
        }

        private void DisplayPublishWizard(Dictionary<string, object> seedProperties)
        {
            var success = false;
            IAWSWizard wizard = null;

            void Invoke() => success = TryCreateWizard(seedProperties, out wizard);

            void Record(ITelemetryLogger telemetryLogger, double duration)
            {
                var connectionSettings = CreateConnectionSettings(wizard);
                RecordEcsPublishWizardMetric(connectionSettings, success, duration);
            }

            _toolkitContext.TelemetryLogger.TimeAndRecord(Invoke, Record);
        }

        private bool TryCreateWizard(Dictionary<string, object> seedProperties, out IAWSWizard wizard)
        {
            var baseDockerImage = DetermineImageBase(seedProperties);

            var navigator = ToolkitFactory.Instance.Navigator;
            LoadPreviousSettings(seedProperties);

            wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.ECS.PublishContainerToAWS", seedProperties);
            wizard.Title = "Publish Container to AWS";
            wizard.SetSelectedAccount(navigator.SelectedAccount, PublishContainerToAWSWizardProperties.UserAccount);
            wizard.SetSelectedRegion(navigator.SelectedRegion, PublishContainerToAWSWizardProperties.Region);

            var defaultPages = new IAWSWizardPageController[]
            {
                new PushImageToECRPageController(),
                new ECSClusterPageController(),
                new ScheduleTaskPageController(),
                new RunTaskPageController(),
                new ECSServicePageController(),
                new ConfigureLoadBalancerPageController(),
                new ECSTaskDefinitionPageController(),
                new PublishProgressPageController(_toolkitContext)
            };

            wizard.RegisterPageControllers(defaultPages, 0);
            wizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Publish");
            wizard.SetShortCircuitPage(AWSWizardConstants.WizardPageReferences.LastPageID);

            wizard.Run();
            var success = wizard.IsPropertySet(PublishContainerToAWSWizardProperties.WizardResult) && (bool) wizard[PublishContainerToAWSWizardProperties.WizardResult];
            _results = new ActionResults().WithSuccess(success);

            return success;
        }

        private AwsConnectionSettings CreateConnectionSettings(IAWSWizard wizard)
        {
            var identifier = wizard.GetSelectedAccount(PublishContainerToAWSWizardProperties.UserAccount)?.Identifier;
            var region = wizard.GetSelectedRegion(PublishContainerToAWSWizardProperties.Region);
            return new AwsConnectionSettings(identifier, region);
        }

        private string DetermineImageBase(Dictionary<string, object> seedProperties)
        {
            try
            {
                if (!seedProperties.ContainsKey(PublishContainerToAWSWizardProperties.SourcePath))
                    return null;

                var dockerFilePath = Path.Combine(seedProperties[PublishContainerToAWSWizardProperties.SourcePath] as string, "Dockerfile");
                if (!File.Exists(dockerFilePath))
                    return null;

                using (var reader = new StreamReader(dockerFilePath))
                {
                    string line;
                    while((line = reader.ReadLine()) != null)
                    {
                        if (!line.StartsWith("FROM "))
                            continue;

                        var tokens = line.Split(' ');
                        if(tokens.Length > 1)
                        {
                            return tokens[1];
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private void RecordEcsPublishWizardMetric(AwsConnectionSettings connectionSettings, bool result, double duration)
        {
            _toolkitContext.TelemetryLogger.RecordEcsPublishWizard(new EcsPublishWizard()
            {
                AwsAccount = connectionSettings.GetAccountId(_toolkitContext.ServiceClientManager) ?? MetadataValue.Invalid,
                AwsRegion = connectionSettings.Region?.Id ?? MetadataValue.Invalid,
                Result = result ? Result.Succeeded : Result.Cancelled,
                Duration = duration
            });
        }
    }
}
