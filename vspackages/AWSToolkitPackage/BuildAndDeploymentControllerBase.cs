using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

using Amazon.AWSToolkit.VisualStudio.BuildProcessors;
using Amazon.AWSToolkit.VisualStudio.DeploymentProcessors;
using Amazon.AWSToolkit.VisualStudio.Loggers;
using Amazon.AWSToolkit.VisualStudio.Shared;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// Base class wrapping the processes of build and deployment 
    /// for Visual Studio web application and web site projects to AWS.
    /// </summary>
    internal abstract class BuildAndDeploymentControllerBase
    {
        protected BuildAndDeploymentControllerBase(Dispatcher dispatcher)
        {
            CallerDispatcher = dispatcher;
        }

        protected BuildAndDeploymentControllerBase() { }

        public IBuildProcessor BuildProcessor { get; set; }
        public IDeploymentProcessor DeploymentProcessor { get; set; }
        public VSWebProjectInfo ProjectInfo { get; set; }
        public IDictionary<string, object> Options { get; set; }
        public object ServicePlugin { get; set; }
        public IBuildAndDeploymentLogger Logger { get; set; }

        public delegate void CompletionCallbackDelegate(bool succeeded);
        public CompletionCallbackDelegate OnCompletionCallback { get; set; }

        public delegate object ServiceProviderDelegate(Type serviceType);
        public ServiceProviderDelegate HostServiceProvider { get; set; }

        protected Dispatcher CallerDispatcher;

        /// <summary>
        /// Takes the specified project and spins off a thread to sequence the 
        /// necessary build and deployment steps using the attached build and 
        /// deployment processors.
        /// </summary>
        /// <remarks>
        /// Callers responsibility to attach the correct build processor for
        /// the project type.
        /// </remarks>
        public void Execute()
        {
            // as we are using the IDE's DTE automation model in our build, which is
            // COM-based, we need an STA thread (threadpool threads are all MTA and
            // can't be switched)
            var execThread = new Thread(this.ExecutionWorker);
            execThread.SetApartmentState(ApartmentState.STA);
            execThread.Start(this);
            //ThreadPool.QueueUserWorkItem(ExecutionWorker, this);
        }

        /// <summary>
        /// Worker thread that does the sequencing
        /// </summary>
        protected void ExecutionWorker(object stateInfo)
        {
            var controller = stateInfo as BuildAndDeploymentControllerBase;
            bool succeeded = true;
            using (var completionEvent = new AutoResetEvent(false))
            {
                try
                {
                    Logger.OutputMessage(string.Format("Publishing '{0}' to Amazon Web Services", ProjectInfo.ProjectName), true);

                    // electing to use non Task approach so code can be re-used in vs2008
                    var bti = ConstructBuildTaskInfo(completionEvent);

                    var buildAttempt = 1;
                    const int maxRetries = 10;
                    var retryBuild = false;
                    do
                    {
                        var buildThread = new Thread(BuildWorker);
                        buildThread.SetApartmentState(ApartmentState.STA);
                        buildThread.Start(new object[] { BuildProcessor, bti });

                        completionEvent.WaitOne();

                        switch (BuildProcessor.Result)
                        {
                            case BuildProcessorBase.ResultCodes.Succeeded:
                                {
                                    // other logged output tells the user this has succeeded, no need for a message
                                    retryBuild = false;
                                }
                                break;

                            case BuildProcessorBase.ResultCodes.FailedShouldRetry:
                                {
                                    if (buildAttempt <= maxRetries)
                                    {
                                        buildAttempt++;
                                        var msg = string.Format("..build failed, attempting retry ({0} of {1} attempts)", buildAttempt, maxRetries);
                                        Logger.OutputMessage(msg);

                                        Thread.Sleep(1000);

                                        bti.BuildAttempt = buildAttempt;
                                        retryBuild = true;
                                    }
                                    else
                                    {
                                        Logger.OutputMessage("..retry attempts exhausted, abanding build");
                                        retryBuild = false;
                                    }
                                }
                                break;

                            case BuildProcessorBase.ResultCodes.Failed:
                                {
                                    retryBuild = false;
                                    Logger.OutputMessage("..build of project archive failed, abandoning deployment", true);
                                    ToolkitFactory.Instance.ShellProvider.ShowError("Publish Failed", "Deployment of the application failed due to errors during build of the project or deployment archive.\r\n\r\nCheck the Output window 'Amazon Web Services' pane for more detail.");
                                }
                                break;
                        }
                    } while (retryBuild);

                    if (BuildProcessor.Result == BuildProcessorBase.ResultCodes.Succeeded)
                    {
                        var dti = ConstructDeploymentTaskInfo(completionEvent);

                        // again, as we could be interacting with IDE objects we'd do best to 
                        // stick with an STA thread
                        var deployThread = new Thread(DeploymentWorker);
                        deployThread.SetApartmentState(ApartmentState.STA);
                        deployThread.Start(new object[] { DeploymentProcessor, dti });

                        completionEvent.WaitOne();
                    }
                }
                catch (Exception exc)
                {
                    Logger.OutputMessage(string.Format("....caught exception during build/deploy process - {0}", exc.Message), true);
                    succeeded = false;
                }
                finally
                {
                    if (controller.OnCompletionCallback != null)
                    {
                        Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
                        {
                            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            controller.OnCompletionCallback(succeeded);
                        });
                    }
                }
            }
        }

        protected abstract BuildTaskInfo ConstructBuildTaskInfo(AutoResetEvent completionEvent);

        protected abstract DeploymentTaskInfo ConstructDeploymentTaskInfo(AutoResetEvent completionEvent);

        // wrapper around ThreadPool workitem to satisfy WaitCallback delegate signature
        protected void BuildWorker(object state)
        {
            var buildProcessor = (state as object[])[0] as IBuildProcessor;
            var buildTaskInfo = (state as object[])[1] as BuildTaskInfo;

            buildProcessor.Build(buildTaskInfo);
        }

        // wrapper around ThreadPool workitem to satisfy WaitCallback delegate signature
        protected void DeploymentWorker(object state)
        {
            var deploymentProcessor = (state as object[])[0] as IDeploymentProcessor;
            var deploymentTaskInfo = (state as object[])[1] as DeploymentTaskInfo;

            deploymentProcessor.DeployPackage(deploymentTaskInfo);
        }
    }
}
