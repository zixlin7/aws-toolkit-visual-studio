using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.ECS.DeploymentWorkers
{
    public class EcsDeployState
    {
        public AccountViewModel Account { get; set; }
        public ToolkitRegion Region { get; set; }
        public string WorkingDirectory { get; set; }

        public IAWSWizard HostingWizard { get; set; }

        public bool? PersistConfigFile { get; set; }
    }
}
