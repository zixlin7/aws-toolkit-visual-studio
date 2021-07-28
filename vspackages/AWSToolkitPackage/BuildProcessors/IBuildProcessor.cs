using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.Shell.Interop;
using Amazon.AWSToolkit.VisualStudio.Loggers;
using Amazon.AWSToolkit.VisualStudio.Shared;

namespace Amazon.AWSToolkit.VisualStudio.BuildProcessors
{
    /// <summary>
    /// Implemented by build processors to enable different build
    /// mechanisms for web site vs web application projects etc
    /// </summary>
    public interface IBuildProcessor
    {
        /// <summary>
        /// Perform a build of the selected project.
        /// </summary>
        /// <remarks>
        /// Called on a secondary thread from the build controller; the processor
        /// should signal completion on the handle contained in BuildTaskInfo 
        /// instance to allow the controller to resume work.
        /// </remarks>
        void Build(BuildTaskInfo buildTaskInfo);

        /// <summary>
        /// Called by the controller when completion event handle signalled 
        /// to gather the result of the build process.
        /// </summary>
        BuildProcessorBase.ResultCodes Result { get; }

        /// <summary>
        /// On successful build, returns the path and name of the package
        /// to be deployed
        /// </summary>
        string DeploymentPackage { get; }
    }

    /// <summary>
    /// Wraps information needed by build processor subtask
    /// </summary>
    public class BuildTaskInfo
    {
        public BuildTaskInfo(BuildAndDeploymentControllerBase.ServiceProviderDelegate hostServiceProvider,
                            VSWebProjectInfo projectInfo,
                            IBuildAndDeploymentLogger logger,
                            IDictionary<string, object> options,
                            string versionLabel,
                            string targetFramework,
                            bool useIncrementalDeployment,
                            AutoResetEvent completionSignalEvent)
        {
            this.HostServiceProvider = hostServiceProvider;
            this.ProjectInfo = projectInfo;
            this.Logger = logger;
            this.Options = options;
            this.VersionLabel = versionLabel;
            this.TargetFramework = targetFramework;
            this.UseIncrementalDeployment = useIncrementalDeployment;
            this.CompletionSignalEvent = completionSignalEvent;
            this.BuildAttempt = 1;
        }

        private BuildTaskInfo() { }

        public BuildAndDeploymentControllerBase.ServiceProviderDelegate HostServiceProvider { get; protected set; }
        public VSWebProjectInfo ProjectInfo { get; protected set; }
        public IBuildAndDeploymentLogger Logger { get; protected set; }
        public string VersionLabel { get; protected set; }
        public string TargetFramework { get; protected set; }
        public IDictionary<string, object> Options { get; protected set; }
        public AutoResetEvent CompletionSignalEvent { get; protected set; }
        public bool UseIncrementalDeployment { get; protected set; }
        public int BuildAttempt { get; set; }   // one-based index

        public bool IsFirstAttempt => BuildAttempt == 1;

        public string TargetRuntime
        {
            get
            {
                if (this.ProjectInfo.VsProjectType == VSWebProjectInfo.VsWebProjectType.CoreCLRWebProject)
                    throw new System.InvalidOperationException("Use TargetFramework member for CoreCLR projects");

                if (this.TargetFramework.StartsWith("4"))
                    return this.TargetFramework;
                else
                    return "2.0";
            }
        }
    }

    /// <summary>
    /// Helper methods and properties for the various build processor implementations
    /// </summary>
    public class BuildProcessorBase
    {
        private readonly object _syncLock = new object();

        public enum ResultCodes
        {
            Failed,
            FailedShouldRetry,
            Succeeded
        }

        protected ResultCodes ProcessorResult = ResultCodes.Failed;

        protected BuildTaskInfo TaskInfo { get; set; }

        private bool? _buildStageSucceeded;
        protected bool? BuildStageSucceeded
        {
            get
            {
                bool? ret;
                lock (_syncLock)
                {
                    ret = _buildStageSucceeded;
                }
                return ret;
            }

            set
            {
                lock (_syncLock)
                {
                    _buildStageSucceeded = value;
                }
            }
        }

        IVsStatusbar _statusBar;
        object _icon;
        bool _hasLockedStatusText;

        IVsStatusbar StatusBar
        {
            get
            {
                return Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run<IVsStatusbar>(async () =>
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    if (_statusBar == null && TaskInfo.HostServiceProvider != null)
                    {
                        _statusBar = (IVsStatusbar)TaskInfo.HostServiceProvider(typeof(SVsStatusbar));
                    }

                    return _statusBar;
                });
            }
        }

        /// <summary>
        /// Triggers animated icon and locks status bar to show the supplied text for the duration of
        /// an operation
        /// </summary>
        /// <param name="feedbackIcon"></param>
        /// <param name="statusText"></param>
        protected void StartStatusBarBuildFeedback(short feedbackIcon, string statusText)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var statusBar = StatusBar;
                if (statusBar == null)
                    return;

                statusBar.Animation(1, ref _icon);
                if (!string.IsNullOrEmpty(statusText))
                {
                    int alreadyFrozen;
                    statusBar.IsFrozen(out alreadyFrozen);

                    if (alreadyFrozen == 0)
                    {
                        statusBar.SetText(statusText);
                        statusBar.FreezeOutput(1);
                        _hasLockedStatusText = true;
                    }
                }
            });
        }

        protected void EndStatusBarBuildFeedback()
        {
            var statusBar = StatusBar;
            if (statusBar == null)
                return;

            Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                statusBar.Animation(0, ref _icon);

                if (_hasLockedStatusText)
                {
                    statusBar.FreezeOutput(0);
                    statusBar.Clear();
                }
            });
        }

        protected void WaitOnBuildCompletion(IVsSolutionBuildManager solnBuildManager)
        {
            // Have to wait on two things - firstly that the build manager has completed
            // and secondly that VS has called us back to signal completion state. 
            // We can't guarantee the order though...
            while (true)
            {
                var buildBusy = Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run<int>(async () =>
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    solnBuildManager.QueryBuildManagerBusy(out int value);
                    return value;
                });
                if (buildBusy == 0)
                {
                    if (BuildStageSucceeded != null)
                        break;
                }
                Thread.Sleep(500);
            }
        }
    }
}
