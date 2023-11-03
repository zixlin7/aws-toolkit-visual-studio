using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Configuration;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Lsp;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

using log4net;

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using StreamJsonRpc;
using Microsoft.VisualStudio.Shell;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients
{
    /// <summary>
    /// Handles the boilerplate configuration for creating a language server and
    /// providing it to Visual Studio.
    ///
    /// LSP Clients are implementations of ILanguageClient. Visual Studio locates
    /// the language clients by searching for MEF Exports of ILanguageClient. VS then
    /// orchestrates and manages the lifecycle of ILanguageClient implementations.
    ///
    /// More information about LSP extensibility in Visual Studio:
    /// https://learn.microsoft.com/en-us/visualstudio/extensibility/adding-an-lsp-extension?view=vs-2022
    /// </summary>
    /// <remarks>
    /// When we have more than one language server, this class will need to get moved to some sort of
    /// "Shared Language Server" project.
    /// </remarks>
    public abstract class ToolkitLspClient : IToolkitLspClient, ILanguageClient, ILanguageClientCustomMessage2
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ToolkitLspClient));

        protected readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        /// <summary>
        /// Responds to JSON-RPC messages sent from the language server.
        /// Exposed to Visual Studio via <see cref="CustomMessageTarget"/>
        /// Set with <see cref="SetLspMessageHandler"/>
        /// </summary>
        private LspMessageHandler _messageHandler;

        [Import]
        protected IToolkitContextProvider _toolkitContextProvider;
        protected ToolkitContext _toolkitContext => _toolkitContextProvider.GetToolkitContext();

        [Import]
        protected ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;
        protected CancellationToken _disposalToken => _taskFactoryProvider.DisposalToken;

        [Import] protected SVsServiceProvider _serviceProvider;

        /// <summary>
        /// Implementations should set this to true if they want this class to orchestrate Credentials initialization.
        /// </summary>
        protected readonly bool _initializeServerWithCredentials = false;

        private readonly CredentialsEncryption _credentialsEncryption = new CredentialsEncryption();

        protected string _serverPath { get; set; }

        /// <summary>
        /// Used to send notifications and requests to the language server
        /// </summary>
        protected JsonRpc _rpc { get; private set; }

        protected ToolkitLspClient(bool initializeServerWithCredentials = false)
        {
            _initializeServerWithCredentials = initializeServerWithCredentials;

            StopAsync += OnStopAsync;
        }

        private Task OnStopAsync(object sender, EventArgs args)
        {
            UnRegisterLspMessageHandler();
            return Task.CompletedTask;
        }

        public event AsyncEventHandler<EventArgs> InitializedAsync;
        public event AsyncEventHandler<WorkspaceConfigurationEventArgs> RequestWorkspaceConfigurationAsync;

        /// <summary>
        /// Requests a JSON-PRC Proxy that is used to access set of
        /// notifications/requests on the language server.
        /// See also: https://github.com/microsoft/vs-streamjsonrpc/blob/main/doc/index.md
        /// </summary>
        /// <typeparam name="TProxy">The interface proxy to request</typeparam>
        protected TProxy CreateProxy<TProxy>() where TProxy : class
        {
            var proxyOptions = new JsonRpcProxyOptions()
            {
                MethodNameTransform = JsonRpcProxy.MethodNameTransform<TProxy>,
            };

            return _rpc.Attach<TProxy>(proxyOptions);
        }

        /// <summary>
        /// Produces the abstraction capable of handling the language server's credentials messages 
        /// </summary>
        public IToolkitLspCredentials CreateToolkitLspCredentials()
        {
            return new ToolkitLspCredentials(_credentialsEncryption, CreateProxy<ILspCredentials>());
        }

        /// <summary>
        /// Produces the abstraction capable of handling the language server's configuration messages 
        /// </summary>
        public ILspConfiguration CreateLspConfiguration()
        {
            return new LspConfiguration(_rpc);
        }

        public event AsyncEventHandler<EventArgs> StartAsync;
#pragma warning disable CS0067 // The event 'ToolkitLspClient.StopAsync' is never used
        public event AsyncEventHandler<EventArgs> StopAsync;
#pragma warning restore CS0067 // The event 'ToolkitLspClient.StopAsync' is never used

        /// <summary>
        /// User-facing name of the Language Client.
        /// This name is shown to users. For example, if the language server writes
        /// to the lsp console, that content will be displayed in an Output Window
        /// that is given this name.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// <inheritdoc/>
        ///
        /// This lets us optionally define JSON files that configure some of the
        /// LSP Client's behavior. See:
        /// https://learn.microsoft.com/en-us/visualstudio/extensibility/adding-an-lsp-extension?view=vs-2022#settings
        /// </summary>
        public virtual IEnumerable<string> ConfigurationSections { get; } = null;

        /// <summary>
        /// <inheritdoc/>
        ///
        /// This is the optional initializationOptions field that is sent to the
        /// language server as part of the "Initialize" request.
        /// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#initialize
        /// </summary>
        public virtual object InitializationOptions { get; } = new { };

        public virtual IEnumerable<string> FilesToWatch { get; } = Enumerable.Empty<string>();

        public bool ShowNotificationOnInitializeFailed { get; } = true;

        /// <summary>
        /// <inheritdoc/>
        /// 
        /// Visual Studio Calls this when the extension has loaded.
        /// </summary>
        public virtual async Task OnLoadedAsync()
        {
            _logger.Info($"Starting set up for language server: {Name}");

            // TODO : Will this block the IDE while waiting?
            await _toolkitContextProvider.WaitForToolkitContextAsync();

            _toolkitContext.ToolkitHost.OutputToHostConsole($"Initializing: {Name}");

            SetupLspMessageHandler();

            // Determines the latest version of the language server, install and return it's path
            var taskStatusNotifier = await CreateTaskStatusNotifierAsync();
            taskStatusNotifier.ShowTaskStatus(async _ =>
            {
                using (var token = CreateCancellationTokenSource(taskStatusNotifier))
                {
                    var lspInstallResult = await InstallServerAsync(taskStatusNotifier, token.Token);
                    _serverPath = lspInstallResult?.Path;

                    // TODO : Have a separate controller responsible for starting the language server
                    await LaunchServerAsync(taskStatusNotifier);
                }
            });
        }

        private async Task LaunchServerAsync(ITaskStatusNotifier taskNotifier)
        {
            taskNotifier.ProgressText = "Launching Language Server...";
            await StartAsync.InvokeAsync(this, EventArgs.Empty);
        }

        /// <summary>
        /// Configures the language client with handlers for custom messages originating from the language server
        /// </summary>
        private void SetupLspMessageHandler()
        {
            var messageHandler = CreateLspMessageHandler();
            messageHandler.WorkspaceConfigurationAsync += OnRequestWorkspaceConfigurationAsync;
            SetLspMessageHandler(messageHandler);
        }

        /// <summary>
        /// Produces the custom message handler.
        /// Clients can override this method in order to handle service-specific messages
        /// in addition to the standard messages supported by <see cref="LspMessageHandler"/>.
        /// </summary>
        protected virtual LspMessageHandler CreateLspMessageHandler()
        {
            return new LspMessageHandler();
        }

        /// <summary>
        /// Handles when the language server sends a request for the configuration state, and
        /// we want to send a response back. The contents of <see cref="WorkspaceConfigurationEventArgs.Response"/>
        /// are sent after this handler completes.
        /// </summary>
        private async Task OnRequestWorkspaceConfigurationAsync(object sender, WorkspaceConfigurationEventArgs args)
        {
            var asyncHandler = RequestWorkspaceConfigurationAsync;
            if (asyncHandler != null)
            {
                await asyncHandler.InvokeAsync(this, args);
            }
        }

        private void SetLspMessageHandler(LspMessageHandler messageHandler)
        {
            UnRegisterLspMessageHandler();
            _messageHandler = messageHandler;
        }

        private void UnRegisterLspMessageHandler()
        {
            var messageHandler = _messageHandler;
            if (messageHandler != null)
            {
                messageHandler.WorkspaceConfigurationAsync -= OnRequestWorkspaceConfigurationAsync;
            }
        }

        /// <summary>
        /// Creates a cancellation token source representing the task status notifier's cancellation token and the toolkit package.
        /// </summary>
        /// <remarks>
        /// Caller is responsible for disposing the created token source.
        /// </remarks>
        private CancellationTokenSource CreateCancellationTokenSource(ITaskStatusNotifier notifier)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(
                notifier.CancellationToken,
                _disposalToken);
        }

        /// <summary>
        /// <inheritdoc/>
        /// 
        /// VS Calls this to start up the Language Server and get the communications streams
        /// </summary>
        /// <returns>
        /// <inheritdoc/>
        /// </returns>
        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            _toolkitContext.ToolkitHost.OutputToHostConsole($"Activating: {Name}");

            await Task.Yield();

            await TaskScheduler.Default;

            var serverProcess = CreateLspProcess();

            _logger.Info($"Launching language server: {Name}");
            if (!serverProcess.Start())
            {
                // null indicates the server cannot be started
                return null;
            }

            await OnBeforeLspConnectionStartsAsync(serverProcess);

            return new Connection(serverProcess.StandardOutput.BaseStream, serverProcess.StandardInput.BaseStream);
        }

        /// <summary>
        /// Creates the child process representing the language server.
        /// Caller is responsible for starting the process.
        /// </summary>
        private Process CreateLspProcess()
        {
            var info = new ProcessStartInfo
            {
                WorkingDirectory = GetServerWorkingDir(),
                FileName = GetServerPath(),
                Arguments = string.Join(" ", GetLspProcessArgs()),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var process = new Process
            {
                StartInfo = info,
            };

            return process;
        }

        /// <summary>
        /// Installs the language server and returns the install location
        /// </summary>
        /// <returns></returns>

        protected abstract Task<LspInstallResult> InstallServerAsync(ITaskStatusNotifier taskNotifier, CancellationToken token = default);

        /// <summary>
        /// Creates the task status notifier
        /// </summary>
        protected abstract Task<ITaskStatusNotifier> CreateTaskStatusNotifierAsync();

        /// <summary>
        /// The folder the language server process should be started from
        /// </summary>
        protected abstract string GetServerWorkingDir();

        /// <summary>
        /// The command to launch the language server
        /// </summary>
        /// <returns></returns>
        protected abstract string GetServerPath();

        /// <summary>
        /// Command line arguments to pass into the language server
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<string> GetLspProcessArgs()
        {
            var args = new List<string>()
            {
                "--stdio",
            };

            if (_initializeServerWithCredentials)
            {
                args.Add("--set-credentials-encryption-key");
            }

            return args;
        }

        /// <summary>
        /// Used by implementing classes that need to interact with the language server process prior to 
        /// starting up the LSP protocol.
        /// </summary>
        /// <param name="lspProcess">Language server process</param>
        protected virtual Task OnBeforeLspConnectionStartsAsync(Process lspProcess)
        {
            if (_initializeServerWithCredentials)
            {
                WriteCredentialsEncryptionInit(lspProcess.StandardInput);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sends the Credentials encryption initialization message to the server
        /// </summary>
        private void WriteCredentialsEncryptionInit(StreamWriter writer)
        {
            var message = _credentialsEncryption.CreateEncryptionInitializationRequest();

            var json = JsonConvert.SerializeObject(message, _jsonSerializerSettings);

            writer.WriteLine(json);
        }

        /// <summary>
        /// VS calls this after successfully making initialization calls with the language server
        /// </summary>
        /// <inheritdoc cref="ILanguageClient.OnServerInitializedAsync"/>
        public async Task OnServerInitializedAsync()
        {
            _logger.Info($"Language server initialization handshake completed: {Name}");
            _toolkitContext.ToolkitHost.OutputToHostConsole($"Initialized: {Name}");

            await RaiseInitializedAsync();
        }

        /// <summary>
        /// Signals to any listeners that the language server has successfully completed the Initialization handshake.
        /// </summary>
        private async Task RaiseInitializedAsync()
        {
            var initializedAsync = InitializedAsync;
            if (initializedAsync != null)
            {
                await initializedAsync.InvokeAsync(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// VS calls this if it was not successful in making initialization calls with the language server
        /// </summary>
        /// <inheritdoc cref="ILanguageClient.OnServerInitializeFailedAsync"/>
        public Task<InitializationFailureContext> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
        {
            _toolkitContext.ToolkitHost.OutputToHostConsole($"Failed to initialize Language Server: {Name}");
            _toolkitContext.ToolkitHost.OutputToHostConsole($"- Status: {initializationState.Status}");
            _toolkitContext.ToolkitHost.OutputToHostConsole($"- Status Message: {initializationState.StatusMessage}");
            _toolkitContext.ToolkitHost.OutputToHostConsole($"- Exception: {initializationState.InitializationException?.Message}");

            _logger.Error($"Failed to initialize Language Server: {Name}", initializationState.InitializationException);
            _logger.Error($"- Status: {initializationState.Status}");
            _logger.Error($"- StatusMessage: {initializationState.StatusMessage}");
            _logger.Error($"- Was server initialized: {initializationState.IsInitialized}");

            var failureInfo = new InitializationFailureContext()
            {
                FailureMessage = initializationState.StatusMessage ??
                                 $"Failed to initialize language server {Name}: {initializationState.InitializationException?.Message}",
            };

            return Task.FromResult(failureInfo);
        }

        #region ILanguageClientCustomMessage2


        public object MiddleLayer => null;

        /// <summary>
        /// Allows extensions to handle LSP messages
        /// Must be set before starting the language server (occurs in the base class OnLoadedAsync)
        /// https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.languageserver.client.ilanguageclientcustommessage2.custommessagetarget?view=visualstudiosdk-2022#microsoft-visualstudio-languageserver-client-ilanguageclientcustommessage2-custommessagetarget
        /// </summary>
        /// <remarks>
        /// If left null, then custom messages won't be delivered.
        /// </remarks>
        public object CustomMessageTarget => _messageHandler;

        public Task AttachForCustomMessageAsync(JsonRpc rpc)
        {
            _rpc = rpc;

            // This is intended for development assistance only!
            // Uncomment this when you need to diagnose messages sent to/from the language server.
// #if DEBUG
        // TODO : IDE-11903 : Comment out LSP trace logging after security testing concludes
             _rpc.LogJsonRpcMessages(this, _toolkitContext.ToolkitHost);
// #endif

            return Task.CompletedTask;
        }

        #endregion
    }
}
