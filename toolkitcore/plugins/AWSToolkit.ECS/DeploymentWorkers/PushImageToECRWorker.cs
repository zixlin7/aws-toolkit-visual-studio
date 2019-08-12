using System;
using Amazon.ECR;
using Amazon.ECS.Tools.Commands;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class PushImageToECRWorker : BaseWorker
    {
        IAmazonECR _ecrClient;

        public PushImageToECRWorker(IDockerDeploymentHelper helper, IAmazonECR ecrClient)
            : base(helper, null)
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
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.ECSPushImage, "Success");
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                    this.Helper.SendCompleteSuccessAsync(state);
                    if (state.PersistConfigFile.GetValueOrDefault())
                        base.PersistDeploymentMode(state.HostingWizard);
                }
                else
                {
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(AttributeKeys.ECSPushImage, command.LastToolsException is ToolsException ? ((ToolsException)command.LastToolsException).Code.ToString() : "Unknown");
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                    if (command.LastToolsException != null)
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
