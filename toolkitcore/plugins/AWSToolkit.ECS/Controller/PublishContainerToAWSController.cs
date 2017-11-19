using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CommonUI.Components;

using Amazon.ECS;
using Amazon.ECS.Model;

using log4net;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.ECS.WizardPages.PageControllers;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
using Amazon.ECS.Tools;
using Amazon.Common.DotNetCli.Tools.Options;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class PublishContainerToAWSController
    {
        ILog LOGGER = LogManager.GetLogger(typeof(PublishContainerToAWSController));

        ActionResults _results;

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

                if (defaults.GetValueAsString(ECSDefinedCommandOptions.ARGUMENT_DOCKER_TAG) != null)
                {
                    var fullName = defaults.GetValueAsString(ECSDefinedCommandOptions.ARGUMENT_DOCKER_TAG);

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
                copyValues(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SUBNETS, PublishContainerToAWSWizardProperties.LaunchSubnets);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SECURITYGROUPS, PublishContainerToAWSWizardProperties.LaunchSecurityGroups);
                copyValues(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_ASSIGN_PUBLIC_IP, PublishContainerToAWSWizardProperties.AssignPublicIpAddress);

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
            var navigator = ToolkitFactory.Instance.Navigator;
            seedProperties[PublishContainerToAWSWizardProperties.UserAccount] = navigator.SelectedAccount;
            seedProperties[PublishContainerToAWSWizardProperties.Region] = navigator.SelectedRegionEndPoints;
            LoadPreviousSettings(seedProperties);

            IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.ECS.PublishContainerToAWS", seedProperties);
            wizard.Title = "Publish Container to AWS";

            IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
            {
                new PushImageToECRPageController(),
                new ECSClusterPageController(),
                new ScheduleTaskPageController(),
                new RunTaskPageController(),
                new ECSServicePageController(),
                new ConfigureLoadBalancerPageController(),
                new ECSTaskDefinitionPageController(),
                new PublishProgressPageController()
            };

            wizard.RegisterPageControllers(defaultPages, 0);
            wizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Publish");
            wizard.SetShortCircuitPage(AWSWizardConstants.WizardPageReferences.LastPageID);

            // as the last page of the wizard performs the upload process, and invokes CancelRun
            // to shutdown the UI, we place no stock in the result of Run() and instead will look
            // for a specific property to be true on exit indicating successful upload vs user
            // cancel.
            wizard.Run();
            var success = wizard.IsPropertySet(PublishContainerToAWSWizardProperties.WizardResult) && (bool)wizard[PublishContainerToAWSWizardProperties.WizardResult];
            _results = new ActionResults().WithSuccess(success);

        }
    }
}
