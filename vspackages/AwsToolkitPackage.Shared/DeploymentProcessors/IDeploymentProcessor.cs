using System.Collections.Generic;
using System.Threading;

using Amazon.AwsToolkit.VsSdk.Common;
using Amazon.AWSToolkit.VisualStudio.Loggers;

namespace Amazon.AWSToolkit.VisualStudio.DeploymentProcessors
{
    /// <summary>
    /// Takes the output from a build processor and submits it
    /// to the Beanstalk engine
    /// </summary>
    public interface IDeploymentProcessor
    {
        /// <summary>
        /// Takes the built deployment archive and submits it to Beanstalk
        /// </summary>
        /// <param name="deploymentTaskInfo"></param>
        /// <remarks>
        /// Called on a secondary thread from the build controller; the processor
        /// should signal completion on the handle contained in DeploymentTaskInfo 
        /// instance to allow the controller to resume work.
        /// </remarks>
        void DeployPackage(DeploymentTaskInfo deploymentTaskInfo);

        /// <summary>
        /// Called by the controller when completion event handle signalled 
        /// to gather the result of the deployment process.
        /// </summary>
        bool Result { get; }
    }

    public class DeploymentTaskInfo
    {
        public DeploymentTaskInfo(BuildAndDeploymentControllerBase.ServiceProviderDelegate hostServiceProvider,
                                  object servicePlugin,
                                  VSWebProjectInfo projectInfo,
                                  IBuildAndDeploymentLogger logger,
                                  IDictionary<string, object> options,
                                  string deploymentPackage,
                                  AutoResetEvent completionSignalEvent)
        {
            this.HostServiceProvider = hostServiceProvider;
            this.ServicePlugin = servicePlugin;
            this.ProjectInfo = projectInfo;
            this.Logger = logger;
            this.Options = options;
            this.DeploymentPackage = deploymentPackage;
            this.CompletionSignalEvent = completionSignalEvent;
        }

        private DeploymentTaskInfo() { }

        public BuildAndDeploymentControllerBase.ServiceProviderDelegate HostServiceProvider { get; protected set; }
        public object ServicePlugin { get; protected set; }
        public VSWebProjectInfo ProjectInfo { get; protected set; }
        public IBuildAndDeploymentLogger Logger { get; protected set; }
        public IDictionary<string, object> Options { get; protected set; }
        public string DeploymentPackage { get; protected set; }
        public AutoResetEvent CompletionSignalEvent { get; protected set; }

    }

}
