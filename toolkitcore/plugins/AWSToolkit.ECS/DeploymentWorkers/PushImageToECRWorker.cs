using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.ECR;

using Amazon.ECS.Tools;
using Amazon.ECS.Tools.Commands;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class PushImageToECRWorker : BaseWorker
    {
        IAmazonECR _ecrClient;

        public PushImageToECRWorker(IDockerDeploymentHelper helper, IAmazonECR ecrClient)
            : base(helper)
        {
            this._ecrClient = ecrClient;
        }

        public void Execute(State state)
        {
            try
            {
                var command = new PushDockerImageCommand(new ECSToolLogger(this.Helper), state.WorkingDirectory, new string[0])
                {
                    Profile = state.Account.Name,
                    Region = state.Region.SystemName,

                    DisableInteractive = true,
                    ECRClient = this._ecrClient,

                    PushDockerImageProperties = ConvertToPushDockerImageProperties(state.HostingWizard),

                    PersistConfigFile = state.PersistConfigFile
                };

                if (command.ExecuteAsync().Result)
                {
                    this.Helper.SendCompleteSuccessAsync(state);
                }
                else
                {
                    if(command.LastToolsException != null)
                        this.Helper.SendCompleteErrorAsync("Error publishing container to AWS: " + command.LastToolsException.Message);
                    else
                        this.Helper.SendCompleteErrorAsync("Unknown error publishing container to AWS");
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error pushing to ECR repository.", e);
                this.Helper.SendCompleteErrorAsync("Error pushing to ECR repository: " + e.Message);
            }
        }


        public class State
        {
            public AccountViewModel Account { get; set; }
            public RegionEndPointsManager.RegionEndPoints Region { get; set; }

            public IAWSWizard HostingWizard { get; set; }

            public string WorkingDirectory { get; set; }

            public bool? PersistConfigFile { get; set; }
        }
    }
}
