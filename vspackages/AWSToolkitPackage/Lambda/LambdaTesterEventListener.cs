using System;
using Amazon.AWSToolkit.Lambda;
using Amazon.AWSToolkit.MobileAnalytics;
using EnvDTE;
using log4net;
using Microsoft.VisualStudio.Shell;

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

        private readonly LambdaTesterUsageEmitter _usageEmitter;

        private bool _debugHookRegistered = false;

        // Plugin usage must be lazy loaded in order to avoid "assembly not found" errors
        private readonly Lazy<IAWSLambda> _lambdaPlugin;

        public LambdaTesterEventListener(AWSToolkitPackage hostPackage)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this._hostPackage = hostPackage;

            this._usageEmitter = new LambdaTesterUsageEmitter(SimpleMobileAnalytics.Instance);

            this._lambdaPlugin = new Lazy<IAWSLambda>(() =>
                hostPackage.ToolkitShellProviderService.QueryAWSToolkitPluginService(typeof(IAWSLambda)) as
                    IAWSLambda);

            Initialize();
        }

        private void Initialize()
        {
            try
            {
                LOGGER.Debug("Initializing Lambda Tester event listener");
                ThreadHelper.ThrowIfNotOnUIThread();

                if (!(_hostPackage.GetVSShellService(typeof(EnvDTE.DTE)) is DTE dte))
                {
                    LOGGER.Error("Unable to get DTE. Lambda Tester will not be automatically configured this session.");
                    return;
                }

                _solutionEvents = dte.Events.SolutionEvents;
                _solutionEvents.Opened += OnSolutionOpened;
                _solutionEvents.ProjectAdded += OnProjectAddedToSolution;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error setting up Lambda Tester event listener", e);
            }
        }

        private void OnSolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!(_hostPackage.GetVSShellService(typeof(EnvDTE.DTE)) is DTE dte))
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
            LambdaTesterUtilities.EnsureLambdaTesterConfigured(dte.Solution, _lambdaPlugin.Value);
        }

        private void OnProjectAddedToSolution(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Configure Lambda Tester usage in Project
            LambdaTesterUtilities.EnsureLambdaTesterConfigured(project, _lambdaPlugin.Value);
        }

        private void RegisterDebuggerEvents(_DTE dte)
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

                _usageEmitter.EmitIfLambdaTester(newProcess.Name, newProcess.ProcessID);
            }
            catch (Exception e)
            {
                LOGGER.Info("Error handling Debug Context Change", e);
            }
        }
    }
}