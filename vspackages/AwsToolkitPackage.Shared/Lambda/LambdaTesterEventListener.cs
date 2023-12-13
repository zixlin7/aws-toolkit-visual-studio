using System;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Tasks;

using EnvDTE;

using EnvDTE80;

using log4net;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.Lambda
{
    /// <summary>
    /// Orchestrates Lambda Tester functionality relating to IDE-specific Events
    /// </summary>
    public class LambdaTesterEventListener
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(LambdaTesterEventListener));

        // When registering event handlers against VS Com objects, we have to hold onto
        // the object reference, otherwise the handlers are not fired.
        private SolutionEvents _solutionEvents;
        private DebuggerEvents _debuggerEvents;

        private readonly AWSToolkitPackage _hostPackage;
        private readonly ToolkitContext _toolkitContext;
        private readonly LambdaTesterUsageEmitter _usageEmitter;

        private bool _debugHookRegistered = false;

        public LambdaTesterEventListener(AWSToolkitPackage hostPackage, ToolkitContext toolkitContext)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _hostPackage = hostPackage;
            _toolkitContext = toolkitContext;

            _usageEmitter = new LambdaTesterUsageEmitter(_toolkitContext.TelemetryLogger);

            Initialize();
        }

        private void Initialize()
        {
            try
            {
                LOGGER.Debug("Initializing Lambda Tester event listener");
                ThreadHelper.ThrowIfNotOnUIThread();

                if (!(_hostPackage.GetVSShellService(typeof(EnvDTE.DTE)) is DTE2 dte))
                {
                    LOGGER.Error("Unable to get DTE. Lambda Tester will not be automatically configured this session.");
                    return;
                }

                _solutionEvents = dte.Events.SolutionEvents;

                // Set up the Lambda tester if a solution was opened before this was initialized
                var handleCurrentSolution = dte.Solution.IsOpen;

                _solutionEvents.Opened += OnSolutionOpened;
                _solutionEvents.ProjectAdded += OnProjectAddedToSolution;

                if (handleCurrentSolution)
                {
                    OnSolutionOpened();
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error setting up Lambda Tester event listener", e);
            }
        }

        private void OnSolutionOpened()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await new TaskNotifier().ShowTaskStatusAsync(async () => await OnSolutionOpenedAsync(), _hostPackage);
            }).Task.LogExceptionAndForget();
        }


        private async Task OnSolutionOpenedAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (!(await _hostPackage.GetServiceAsync(typeof(DTE)) is DTE2 dte))
                {
                    LOGGER.Error("Unable to get DTE");
                    return;
                }

                // Listen for Visual Studio Debugger Sessions
                // We don't do this during Toolkit initialization, because it force loads the
                // Debugger module if that hasn't been loaded already, which causes longer
                // Toolkit activation times. Instead, we defer this to the first time a solution
                // is loaded, since it is more likely that VS has loaded the Debugger by then.
                if (!_debugHookRegistered)
                {
                    RegisterDebuggerEvents(dte);
                    _debugHookRegistered = true;
                }

                // Configure Lambda Tester usage in the Solution's Projects
                await LambdaTesterUtilities.EnsureLambdaTesterConfiguredAsync(dte.Solution, ThreadHelper.JoinableTaskFactory);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error trying to set up Lambda tester with solution", e);
            }
        }

        private void OnProjectAddedToSolution(Project project)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await new TaskNotifier().ShowTaskStatusAsync(async () => await OnProjectAddedToSolutionAsync(project), _hostPackage);
            }).Task.LogExceptionAndForget();
            
        }

        private async Task OnProjectAddedToSolutionAsync(Project project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            // Configure Lambda Tester usage in Project
            await LambdaTesterUtilities.EnsureLambdaTesterConfiguredAsync(project, ThreadHelper.JoinableTaskFactory);
        }

        private void RegisterDebuggerEvents(DTE2 dte)
        {
            LOGGER.Debug("Registering Debugger events");
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                _debuggerEvents = dte.Events.DebuggerEvents;
                _debuggerEvents.OnContextChanged += OnDebuggerContextChanged;
            }
            catch (Exception e)
            {
                LOGGER.Error("Unable to register Debugger Events", e);
            }
        }
        
        private void OnDebuggerContextChanged(
            Process newProcess,
            Program newProgram,
            Thread newThread,
            StackFrame newStackFrame)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                //determine configuration
                var config = newProgram.DTE?.Solution?.SolutionBuild?.ActiveConfiguration?.Name ?? "Debug";
                bool debug = string.Equals("Debug", config, StringComparison.OrdinalIgnoreCase);
                _usageEmitter.EmitIfLambdaTester(newProcess.Name, newProcess.ProcessID, debug);
            }
            catch (Exception e)
            {
                LOGGER.Info("Error handling Debug Context Change", e);
            }
        }
    }
}
