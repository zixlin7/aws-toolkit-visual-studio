using System;
using System.Threading;
using System.Windows.Threading;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.VisualStudio.BuildProcessors;
using Amazon.AWSToolkit.VisualStudio.DeploymentProcessors;

namespace Amazon.AWSToolkit.VisualStudio
{
    internal class CloudFormationBuildAndDeployController : BuildAndDeploymentControllerBase 
    {
        public CloudFormationBuildAndDeployController(Dispatcher dispatcher) 
            : base(dispatcher)
        { 
        }

        protected CloudFormationBuildAndDeployController() { }

        protected override BuildTaskInfo ConstructBuildTaskInfo(AutoResetEvent completionEvent)
        {
            // some data is passed in the 'open' so we do not have to bind to package-specific properties
            // in addition, no concept of version label in this deployment scenario, so fake one
            string versionLabel = string.Format("v{0}", DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss"));
            return new BuildTaskInfo
                       (
                            HostServiceProvider,
                            ProjectInfo,
                            Logger,
                            Options,
                            versionLabel,
                            Options[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] as string,
                            false,
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
