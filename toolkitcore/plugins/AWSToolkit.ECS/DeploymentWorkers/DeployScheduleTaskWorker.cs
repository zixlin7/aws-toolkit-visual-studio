using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.CloudWatchEvents;
using Amazon.Common.DotNetCli.Tools;
using Amazon.ECR;
using Amazon.ECS;
using Amazon.ECS.Tools.Commands;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class DeployScheduleTaskWorker : BaseWorker
    {
        IAmazonCloudWatchEvents _cweClient;
        IAmazonECR _ecrClient;
        IAmazonECS _ecsClient;

        public DeployScheduleTaskWorker(IDockerDeploymentHelper helper,
            IAmazonCloudWatchEvents cweClient,
            IAmazonECR ecrClient,
            IAmazonECS ecsClient,
            IAmazonIdentityManagementService iamClient)
            : base(helper, iamClient)
        {
            this._cweClient = cweClient;
            this._ecrClient = ecrClient;
            this._ecsClient = ecsClient;
            this._iamClient = iamClient;
        }

        public void Execute(State state)
        {
            try
            {
                var command = new DeployScheduledTaskCommand(new ECSToolLogger(this.Helper), state.WorkingDirectory, new string[0])
                {
                    Profile = state.Account.Name,
                    Region = state.Region.SystemName,

                    DisableInteractive = true,
                    CWEClient = this._cweClient,
                    ECRClient = this._ecrClient,
                    ECSClient = this._ecsClient,

                    PushDockerImageProperties = ConvertToPushDockerImageProperties(state.HostingWizard),
                    TaskDefinitionProperties = ConvertToTaskDefinitionProperties(state.HostingWizard),
                    DeployScheduledTaskProperties = ConvertToDeployScheduledTaskProperties(state.HostingWizard),
                    ClusterProperties = ConvertToClusterProperties(state.HostingWizard),

                    PersistConfigFile = state.PersistConfigFile
                };

                if(state.HostingWizard[PublishContainerToAWSWizardProperties.CreateCloudWatchEventIAMRole] is bool &&
                    (bool)state.HostingWizard[PublishContainerToAWSWizardProperties.CreateCloudWatchEventIAMRole])
                {
                    var newRoleName = Amazon.Common.DotNetCli.Tools.RoleHelper.GenerateUniqueIAMRoleName(this._iamClient, "ecsEventsRole");
                    this.Helper.AppendUploadStatus("Creating IAM role " + newRoleName + " for CloudWatch Events to use to run the ECS task");
                    var roleArn = Amazon.Common.DotNetCli.Tools.RoleHelper.CreateRole(this._iamClient, newRoleName, Amazon.Common.DotNetCli.Tools.Constants.CWE_ASSUME_ROLE_POLICY, "AmazonEC2ContainerServiceEventsRole");
                    command.DeployScheduledTaskProperties.CloudWatchEventIAMRole = roleArn;
                }

                if (command.ExecuteAsync().Result)
                {
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.ECSDeployScheduleTask, "Success");
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                    this.Helper.SendCompleteSuccessAsync(state);
                }
                else
                {
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.ECSDeployScheduleTask, command.LastToolsException is ToolsException ? ((ToolsException)command.LastToolsException).Code.ToString() : "Unknown");
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                    if (command.LastToolsException != null)
                        this.Helper.SendCompleteErrorAsync("Error deploy scheduled task to AWS: " + command.LastToolsException.Message);
                    else
                        this.Helper.SendCompleteErrorAsync("Unknown error deploy scheduled task to AWS");
                }
            }
            catch (Exception e)
            {
                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.ECSDeployScheduleTask, "Unknown");
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                LOGGER.Error("Error deploy scheduled task.", e);
                this.Helper.SendCompleteErrorAsync("Error deploy scheduled task: " + e.Message);
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
