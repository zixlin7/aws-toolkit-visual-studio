using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.Context;
using Amazon.CloudFormation.Model;
using AWSDeployment;

using log4net;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    class RedeployApplicationController : DeploymentControllerBase
    {
        public RedeployApplicationController(string deploymentPackage, IDictionary<string, object> deploymentProperties, ToolkitContext toolkitContext)
            : base(deploymentPackage, deploymentProperties, toolkitContext)
        {
            var credentials = toolkitContext.CredentialManager.GetAwsCredentials(_account.Identifier, Region);

            Logger = LogManager.GetLogger(typeof(RedeployApplicationController));
            Observer = new DeploymentControllerBaseObserver(Logger);
            Deployment.Observer = Observer;
            Deployment.AWSProfileName = _account.Identifier.ProfileName;
            Deployment.Credentials = credentials;
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
                    {
                        var region = ToolkitContext.RegionProvider.GetRegion(Deployment.Region);
                        bucketName = DefaultBucketName(_account, region);
                    }
                }

                Deployment.UploadBucket = bucketName;

                CopyContainerProperties();
                CopyApplicationOptionProperties();
                Deployment.Redeploy();
            }
            catch (Exception e)
            {
                var errMsg = $"Error redeploying application: {e.Message}";
                ToolkitFactory.Instance.ShellProvider.ShowError("Redeploy Error", errMsg);
            }
        }
    }
}
