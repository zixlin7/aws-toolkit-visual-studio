using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.CloudFormation.Model;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageWorkers;

using AWSDeployment;

using log4net;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    class RedeployApplicationController : DeploymentControllerBase
    {
        public RedeployApplicationController(string deploymentPackage, IDictionary<string, object> deploymentProperties)
            : base(deploymentPackage, deploymentProperties)
        {
            LOGGER = LogManager.GetLogger(typeof(RedeployApplicationController));
            Observer = new DeploymentControllerBaseObserver(LOGGER);
            Deployment.Observer = Observer;
            Deployment.Credentials = _account.Credentials;
            Deployment.DeploymentMode = DeploymentEngineBase.DeploymentModes.RedeployNewVersion;
        }

        public override void Execute()
        {
            try
            {
                Deployment.StackName = DeploymentProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;
                // on redeploy, try and re-use the same bucket/config key as the original stack was created with. Only bucket name
                // is mandatory at this stage.
                string bucketName = null;
                string configFileKey = null;
                if (DeploymentProperties.ContainsKey(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeploymentInstance))
                {
                    ExistingServiceDeployment deployment
                            = DeploymentProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_RedeploymentInstance] as ExistingServiceDeployment;
                    Stack redeploymentStack = deployment.Tag as Stack;
                    CloudFormationUtil.DeterminePriorBucketAndConfigNames(redeploymentStack, out bucketName, out configFileKey);
                    if (string.IsNullOrEmpty(bucketName))
                        bucketName = DefaultBucketName(_account, Deployment.Region);
                }

                Deployment.UploadBucket = bucketName;

                string configFileDestination = getValue<string>(DeploymentWizardProperties.ReviewProperties.propkey_ConfigFileDestination);
                Deployment.ConfigFileDestination = configFileDestination;

                CopyContainerProperties();
                CopyApplicationOptionProperties();
                Deployment.Redeploy();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(e.Message);
            }
        }
    }
}
