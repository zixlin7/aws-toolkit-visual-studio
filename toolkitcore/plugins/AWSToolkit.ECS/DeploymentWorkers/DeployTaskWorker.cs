﻿using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.CloudWatchLogs;
using Amazon.Common.DotNetCli.Tools;
using Amazon.ECR;
using Amazon.ECS;
using Amazon.ECS.Tools.Commands;
using Amazon.IdentityManagement;
using System;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class DeployTaskWorker : BaseWorker
    {
        IAmazonECR _ecrClient;
        IAmazonECS _ecsClient;
        IAmazonCloudWatchLogs _cwlClient;

        public DeployTaskWorker(IDockerDeploymentHelper helper,
            IAmazonECR ecrClient,
            IAmazonECS ecsClient,
            IAmazonIdentityManagementService iamClient,
            IAmazonCloudWatchLogs cwlClient)
            : base(helper, iamClient)
        {
            this._ecrClient = ecrClient;
            this._ecsClient = ecsClient;
            this._iamClient = iamClient;
            this._cwlClient = cwlClient;
        }

        public void Execute(State state)
        {
            try
            {
                var command = new DeployTaskCommand(new ECSToolLogger(this.Helper), state.WorkingDirectory, new string[0])
                {
                    Profile = state.Account.Name,
                    Region = state.Region.SystemName,

                    DisableInteractive = true,
                    ECRClient = this._ecrClient,
                    ECSClient = this._ecsClient,
                    CWLClient = this._cwlClient,

                    PushDockerImageProperties = ConvertToPushDockerImageProperties(state.HostingWizard),
                    TaskDefinitionProperties = ConvertToTaskDefinitionProperties(state.HostingWizard),
                    DeployTaskProperties = ConvertToDeployTaskProperties(state.HostingWizard),
                    ClusterProperties = ConvertToClusterProperties(state.HostingWizard),

                    PersistConfigFile = state.PersistConfigFile
                };

                if (!string.IsNullOrEmpty(command.ClusterProperties?.LaunchType))
                {
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.ECSLaunchType, command.ClusterProperties.LaunchType);
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
                }

                if (command.ExecuteAsync().Result)
                {
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.ECSDeployTask, "Success");
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                    this.Helper.SendCompleteSuccessAsync(state);
                    if (state.PersistConfigFile.GetValueOrDefault())
                        base.PersistDeploymentMode(state.HostingWizard);
                }
                else
                {
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.ECSDeployTask, command.LastToolsException is ToolsException ? ((ToolsException)command.LastToolsException).Code.ToString() : "Unknown");
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);


                    if (command.LastToolsException != null)
                        this.Helper.SendCompleteErrorAsync("Error deploy task to AWS: " + command.LastToolsException.Message);
                    else
                        this.Helper.SendCompleteErrorAsync("Unknown error deploy task to AWS");
                }
            }
            catch (Exception e)
            {
                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.ECSDeployTask, "Unknown");
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                LOGGER.Error("Error deploy task.", e);
                this.Helper.SendCompleteErrorAsync("Error deploy task: " + e.Message);
            }
        }

        public class State
        {
            public AccountViewModel Account { get; set; }
            public RegionEndPointsManager.RegionEndPoints Region { get; set; }
            public string WorkingDirectory { get; set; }

            public IAWSWizard HostingWizard { get; set; }

            public bool? PersistConfigFile { get; set; }
        }
    }
}
