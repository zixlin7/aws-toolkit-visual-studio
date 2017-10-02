using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.ECR;
using Amazon.ECS;

using Amazon.ECS.Tools;
using Amazon.ECS.Tools.Commands;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class DeployToClusterWorker : BaseWorker
    {
        IAmazonECR _ecrClient;
        IAmazonECS _ecsClient;

        public DeployToClusterWorker(IDockerDeploymentHelper helper, IAmazonECR ecrClient, IAmazonECS ecsClient)
            : base(helper)
        {
            this._ecrClient = ecrClient;
            this._ecsClient = ecsClient;
        }

        public void Execute(State state)
        {
            try
            {
                var command = new DeployCommand(new ECSToolLogger(this.Helper), state.WorkingDirectory, new string[0])
                {
                    Profile = state.Account.Name,
                    Region = state.Region.SystemName,

                    DisableInteractive = true,
                    ECRClient = this._ecrClient,
                    ECSClient = this._ecsClient,

                    Configuration = state.Configuration,
                    DockerImageTag = state.DockerImageTag,

                    ECSTaskDefinition = state.TaskDefinition,
                    ECSContainer = state.Container,
                    ContainerMemoryHardLimit = state.MemoryHardLimit,
                    ContainerMemorySoftLimit = state.MemorySoftLimit,

                    ECSCluster = state.Cluster,
                    ECSService = state.Service,
                    DesiredCount = state.DesiredCount,

                    PersistConfigFile = state.PersistConfigFile
                };

                if(state.PortMapping != null && state.PortMapping.Count > 0)
                {
                    string[] mappings = new string[state.PortMapping.Count];
                    for(int i = 0; i < mappings.Length; i++)
                    {
                        mappings[i] = $"{state.PortMapping[i].HostPort}:{state.PortMapping[i].ContainerPort}";
                    }
                    command.PortMappings = mappings;
                }

                if (command.ExecuteAsync().Result)
                {
                    this.Helper.SendCompleteSuccessAsync(state);
                }
                else
                {
                    if (command.LastToolsException != null)
                        this.Helper.SendCompleteErrorAsync("Error publishing container to AWS: " + command.LastToolsException.Message);
                    else
                        this.Helper.SendCompleteErrorAsync("Unknown error publishing container to AWS");
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deploying to ECS Cluster.", e);
                this.Helper.SendCompleteErrorAsync("Error deploying to ECS Cluster: " + e.Message);
            }
        }


        public class State
        {
            public AccountViewModel Account { get; set; }
            public RegionEndPointsManager.RegionEndPoints Region { get; set; }

            public string Configuration { get; set; }
            public string WorkingDirectory { get; set; }
            public string DockerImageTag { get; set; }

            public string TaskDefinition { get; set; }
            public string Container { get; set; }
            public int? MemoryHardLimit { get; set; }
            public int? MemorySoftLimit { get; set; }
            public IList<PortMappingItem> PortMapping { get; set; }

            public string Cluster { get; set; }
            public string Service { get; set; }
            public int DesiredCount { get; set; }

            public bool? PersistConfigFile { get; set; }
        }
    }
}
