using System;
using Amazon.AWSToolkit.ElasticBeanstalk;

namespace Amazon.AWSToolkit.VisualStudio.Shared.DeploymentProcessors
{
    /// <summary>
    /// 'Standard' deployment processor, routing the deployment package
    /// through S3 and onwards to Beanstalk.
    /// </summary>
    internal class BeanstalkDeploymentProcessor : IDeploymentProcessor
    {
        bool _deploymentResult;

        #region IDeploymentProcessor

        void IDeploymentProcessor.DeployPackage(DeploymentTaskInfo taskInfo)
        {
            try
            {
                var beanstalkPlugin = taskInfo.ServicePlugin as IAWSElasticBeanstalk;
                // the deployment package will be a zip file or folder reference depending on incremental mode -
                // the location of the repository to push to is contained in the Options dictionary and doesn't
                // concern us at this level
                _deploymentResult = beanstalkPlugin.DeploymentService.Deploy(taskInfo.DeploymentPackage, taskInfo.Options);
            }
            catch (Exception exc)
            {
                taskInfo.Logger.OutputMessage(string.Format("Caught exception during handoff process to Elastic Beanstalk, deployment failed - {0}", exc.Message));
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
