using System.Threading;
using System.Windows.Threading;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.VisualStudio.BuildProcessors;
using Amazon.AWSToolkit.VisualStudio.DeploymentProcessors;
using Amazon.AWSToolkit.ElasticBeanstalk;

namespace Amazon.AWSToolkit.VisualStudio
{
    internal class BeanstalkBuildAndDeployController : BuildAndDeploymentControllerBase
    {
        bool _useIncrementalDeployment;

        public BeanstalkBuildAndDeployController(Dispatcher dispatcher) 
            : base(dispatcher)
        { 
        }

        protected BeanstalkBuildAndDeployController() { }

        protected override BuildTaskInfo ConstructBuildTaskInfo(AutoResetEvent completionEvent)
        {
            // some data is passed in the 'open' so we do not have to bind to package-specific properties
            if (Options.ContainsKey(BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalDeployment))
                _useIncrementalDeployment = (bool)Options[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalDeployment];

            var versionLabel = string.Empty;
            if (Options.ContainsKey(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel))
                versionLabel = Options[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel] as string;
            else if (Options.ContainsKey(DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel))
                versionLabel = Options[DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel] as string;

            return new BuildTaskInfo
                       (
                            HostServiceProvider,
                            ProjectInfo,
                            Logger,
                            Options,
                            versionLabel,
                            Options[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] as string,
                            _useIncrementalDeployment,
                            completionEvent
                       );
        }

        protected override DeploymentTaskInfo ConstructDeploymentTaskInfo(AutoResetEvent completionEvent)
        {
            return new DeploymentTaskInfo
                       (
                            HostServiceProvider,
                            ServicePlugin,
                            ProjectInfo,
                            Logger,
                            Options,
                            BuildProcessor.DeploymentPackage,
                            completionEvent
                       );
        }
    }
}
