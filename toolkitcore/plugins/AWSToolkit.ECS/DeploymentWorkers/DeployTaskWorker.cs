using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
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
    public class DeployTaskWorker : BaseWorker
    {
        IAmazonECR _ecrClient;
        IAmazonECS _ecsClient;
        IAmazonIdentityManagementService _iamClient;

        public DeployTaskWorker(IDockerDeploymentHelper helper,
            IAmazonECR ecrClient,
            IAmazonECS ecsClient,
            IAmazonIdentityManagementService iamClient)
            : base(helper)
        {
            this._ecrClient = ecrClient;
            this._ecsClient = ecsClient;
            this._iamClient = iamClient;
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

                    PushDockerImageProperties = ConvertToPushDockerImageProperties(state.HostingWizard),
                    TaskDefinitionProperties = ConvertToTaskDefinitionProperties(state.HostingWizard),
                    DeployTaskProperties = ConvertToDeployTaskProperties(state.HostingWizard),
                    ClusterProperties = ConvertToClusterProperties(state.HostingWizard),

                    PersistConfigFile = state.PersistConfigFile
                };

                if (command.ExecuteAsync().Result)
                {
                    this.Helper.SendCompleteSuccessAsync(state);
                }
                else
                {
                    if (command.LastToolsException != null)
                        this.Helper.SendCompleteErrorAsync("Error deploy task to AWS: " + command.LastToolsException.Message);
                    else
                        this.Helper.SendCompleteErrorAsync("Unknown error deploy task to AWS");
                }
            }
            catch (Exception e)
            {
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
