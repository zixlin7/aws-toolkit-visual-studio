using Amazon.AWSToolkit.VisualStudio.BuildProcessors;
using System;

using Amazon.AWSToolkit.ElasticBeanstalk;

namespace Amazon.AWSToolkit.VisualStudio.DeploymentProcessors
{
    /// <summary>
    /// This deployment processor forces the Toolkit to use Amazon.ElasticBeanstalk.Tools to handle deployment instead of the legacy deployment logic.
    /// </summary>
    public class EbToolsDeploymentProcessor : IDeploymentProcessor, IBuildProcessor
    {

        bool _deploymentResult;
        bool IDeploymentProcessor.Result { get { return _deploymentResult; } }

        void IDeploymentProcessor.DeployPackage(DeploymentTaskInfo taskInfo)
        {
            try
            {
                var beanstalkPlugin = taskInfo.ServicePlugin as IAWSElasticBeanstalk;
                /// Passing in project path for deployment package because deferring the creation deployment bundle to the the EB Tools CLI used for deployment.
                _deploymentResult = beanstalkPlugin.DeploymentService.Deploy(taskInfo.ProjectInfo.VsProjectLocation, taskInfo.Options);
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


        #region Stub implementation IBuildProcessor since build package created in CLI Tool.
        BuildProcessorBase.ResultCodes IBuildProcessor.Result => BuildProcessorBase.ResultCodes.Succeeded;

        string IBuildProcessor.DeploymentPackage => null;

        // No build happens here because it will happen later when Amazon.ElasticBeanstalk.Tools is executed to run the deployment.
        void IBuildProcessor.Build(BuildTaskInfo buildTaskInfo)
        {
            buildTaskInfo.CompletionSignalEvent.Set();
        }
        #endregion
    }
}
