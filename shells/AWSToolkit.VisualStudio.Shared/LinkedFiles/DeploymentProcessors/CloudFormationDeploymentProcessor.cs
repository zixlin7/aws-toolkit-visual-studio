using System;
using Amazon.AWSToolkit.CloudFormation;

namespace Amazon.AWSToolkit.VisualStudio.Shared.DeploymentProcessors
{
    /// <summary>
    /// 'Standard' deployment processor, routing the deployment package
    /// through S3 and onwards to CloudFormation.
    /// </summary>
    internal class CloudFormationDeploymentProcessor : IDeploymentProcessor
    {
        bool _deploymentResult;

        #region IDeploymentProcessor

        void IDeploymentProcessor.DeployPackage(DeploymentTaskInfo taskInfo)
        {
            try
            {
                var cloudFormationPlugin = taskInfo.ServicePlugin as IAWSCloudFormation;
                _deploymentResult = cloudFormationPlugin.DeploymentService.Deploy(taskInfo.DeploymentPackage, taskInfo.Options);
            }
            catch (Exception exc)
            {
                taskInfo.Logger.OutputMessage(string.Format("Caught exception during handoff process to CloudFormation, deployment failed - {0}", exc.Message));
            }
            finally
            {
                taskInfo.CompletionSignalEvent.Set();
            }
        }

        bool IDeploymentProcessor.Result 
        {
            get { return _deploymentResult; }
        }

        #endregion
    }
}
