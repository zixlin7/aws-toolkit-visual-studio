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
using Amazon.ECS.Tools.Options;

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
            if (!File.Exists(Path.Combine(sourcePath, DockerToolsDefaultsReader.DEFAULT_FILE_NAME)))
                return;

            try
            {
                var defaults = DockerToolsDefaultsReader.LoadDefaults(sourcePath, DockerToolsDefaultsReader.DEFAULT_FILE_NAME);

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
                                if (commandOption == DefinedCommandOptions.ARGUMENT_ECS_MEMORY_HARD_LIMIT || commandOption == DefinedCommandOptions.ARGUMENT_ECS_MEMORY_SOFT_LIMIT)
                                {
                                    seedValues[seedKey] = new int?(b);
                                }
                                else
                                {
                                    seedValues[seedKey] = b;
                                }
                            }
                        }
                        else
                        {
                            seedValues[seedKey] = defaults.GetValueAsString(commandOption);
                        }
                    }
                };

                if (defaults.GetValueAsString(DefinedCommandOptions.ARGUMENT_DOCKER_TAG) != null)
                {
                    var fullName = defaults.GetValueAsString(DefinedCommandOptions.ARGUMENT_DOCKER_TAG);

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

                copyValues(DefinedCommandOptions.ARGUMENT_CONFIGURATION, PublishContainerToAWSWizardProperties.Configuration);
                copyValues(DefinedCommandOptions.ARGUMENT_ECS_TASK_DEFINITION, PublishContainerToAWSWizardProperties.TaskDefinition);
                copyValues(DefinedCommandOptions.ARGUMENT_ECS_CONTAINER, PublishContainerToAWSWizardProperties.Container);
                copyValues(DefinedCommandOptions.ARGUMENT_ECS_MEMORY_HARD_LIMIT, PublishContainerToAWSWizardProperties.MemoryHardLimit);
                copyValues(DefinedCommandOptions.ARGUMENT_ECS_MEMORY_SOFT_LIMIT, PublishContainerToAWSWizardProperties.MemorySoftLimit);
                copyValues(DefinedCommandOptions.ARGUMENT_ECS_CLUSTER, PublishContainerToAWSWizardProperties.Cluster);
                copyValues(DefinedCommandOptions.ARGUMENT_ECS_SERVICE, PublishContainerToAWSWizardProperties.Service);
                copyValues(DefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT, PublishContainerToAWSWizardProperties.DesiredCount);

                if (defaults.GetValueAsString(DefinedCommandOptions.ARGUMENT_ECS_CONTAINER_PORT_MAPPING) != null)
                {
                    var mappings = new List<WizardPages.PageUI.PortMappingItem>();
                    var portMappingStr = defaults.GetValueAsString(DefinedCommandOptions.ARGUMENT_ECS_CONTAINER_PORT_MAPPING);
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
