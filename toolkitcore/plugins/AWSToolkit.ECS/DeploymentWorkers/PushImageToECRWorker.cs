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
                    DisableInteractive = true,
                    ECRClient = this._ecrClient,

                    Configuration = state.Configuration,
                    DockerImageTag = state.DockerImageTag
                };

                if (command.ExecuteAsync().Result)
                {
                    this.Helper.SendCompleteSuccessAsync(state);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error uploading Lambda function.", e);
                this.Helper.SendCompleteErrorAsync("Error uploading function: " + e.Message);
            }
        }


        public class State
        {
            public AccountViewModel Account { get; set; }
            public RegionEndPointsManager.RegionEndPoints Region { get; set; }

            public string Configuration { get; set; }
            public string WorkingDirectory { get; set; }
            public string DockerImageTag { get; set; }
        }
    }
}
