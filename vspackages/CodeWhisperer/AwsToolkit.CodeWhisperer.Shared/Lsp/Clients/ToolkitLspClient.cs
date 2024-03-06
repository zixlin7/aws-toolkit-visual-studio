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
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Telemetry;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Lsp;

using log4net;

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using StreamJsonRpc;
using Amazon.AwsToolkit.CodeWhisperer.Telemetry;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Tasks;
using Amazon.AwsToolkit.Telemetry.Events.Core;

using AwsToolkit.VsSdk.Common.Settings.Proxy;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

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

        [Import]
        protected IProxySettingsRepository _proxySettingsRepository;

        protected CancellationToken _disposalToken => _taskFactoryProvider.DisposalToken;

        [Import] protected SVsServiceProvider _serviceProvider;

        /// <summary>
        /// Implementations should set this to true if they want this class to orchestrate Credentials initialization.
        /// </summary>
        protected readonly bool _initializeServerWithCredentials = false;

        private readonly CredentialsEncryption _credentialsEncryption = new CredentialsEncryption();

        protected string _serverPath { get; set; }
        private ProcessWatcher _processWatcher;
        private LspClientStatus _lspClientStatus = LspClientStatus.NotRunning;

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
        public event AsyncEventHandler<ConnectionMetadataEventArgs> RequestConnectionMetadataAsync;
        public event EventHandler<TelemetryEventArgs> TelemetryEventNotification;

        public event EventHandler<LspClientStatusChangedEventArgs> StatusChanged;
        public LspClientStatus Status
        {
            get => _lspClientStatus;
            set
            {
                if (_lspClientStatus != value)
                {
                    _lspClientStatus = value;
                    RaiseStatusChanged(value);
                }
            }
        }

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

        public async Task StartClientAsync()
        {
            await _toolkitContextProvider.WaitForToolkitContextAsync();

            await StartServerAsync();
        }

        public async Task StopClientAsync()
        {
            if (_toolkitContextProvider.HasToolkitContext())
            {
                _toolkitContext.ToolkitHost.OutputToHostConsoleAsync($"Stopping: {Name}", false).LogExceptionAndForget();
            }

            await StopAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        /// <summary>
        /// User-facing name of the Language Client.
        /// This name is shown to users. For example, if the language server writes
        /// to the lsp console, that content will be displayed in an Output Window
        /// that is given this name.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Unique identifier for the language server
        /// </summary>
        public abstract string LanguageServerIdentifier { get; }

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
        /// This is run when a solution or folder is opened.
        /// </summary>
        public virtual async Task OnLoadedAsync()
        {
            await _toolkitContextProvider.WaitForToolkitContextAsync();

            if (!await IsEnabledAsync())
            {
                return;
            }

            await StartClientAsync();
        }

        protected virtual Task<bool> IsEnabledAsync()
        {
            return Task.FromResult(true);
        }

        private async Task StartServerAsync()
        {
            _logger.Info($"Starting set up for language server: {Name}");
            Status = LspClientStatus.SettingUp;

            _toolkitContext.ToolkitHost.OutputToHostConsoleAsync($"Initializing: {Name}", false).LogExceptionAndForget();

            SetupLspMessageHandler();

            // Determines the latest version of the language server, install and return it's path
            var taskStatusNotifier = await CreateTaskStatusNotifierAsync();
            taskStatusNotifier.ShowTaskStatus(async _ =>
            {
                using (var token = CreateCancellationTokenSource(taskStatusNotifier))
                {
                    await SetupServerAsync(taskStatusNotifier, token);
                }
            });
        }

        private async Task SetupServerAsync(ITaskStatusNotifier notifier, CancellationTokenSource token)
        {
            async Task<LspInstallResult> ExecuteAsync() => await InstallAndLaunchServerAsync(notifier, token);

            void RecordSetupFull(ITelemetryLogger telemetryLogger, LspInstallResult result, TaskResult taskResult, long milliseconds)
            {
                //set status to fail if path is unset
                if (taskResult.Status == TaskStatus.Success)
                {
                    taskResult.Status = string.IsNullOrWhiteSpace(result.Path) ? TaskStatus.Fail : TaskStatus.Success;
                }
               
                var args = LspInstallUtil.CreateRecordLspInstallerArgs(result, milliseconds);
                args.Id = LanguageServerIdentifier;
                telemetryLogger.RecordSetupAll(taskResult, args);
            }

            await _toolkitContext.TelemetryLogger.ExecuteTimeAndRecordAsync(ExecuteAsync, RecordSetupFull);
        }

        private async Task<LspInstallResult> InstallAndLaunchServerAsync(ITaskStatusNotifier taskStatusNotifier, CancellationTokenSource token)
        {
            try
            {
                var lspInstallResult = await InstallServerAsync(taskStatusNotifier, token.Token);
                _serverPath = lspInstallResult?.Path;

                // TODO : Have a separate controller responsible for starting the language server
                await LaunchServerAsync(lspInstallResult, taskStatusNotifier);
                return lspInstallResult;
            }
            catch (Exception e)
            {
                Status = LspClientStatus.Error;
                _logger.Error("Unable to install language server", e);

                throw;
            }

        }

        private async Task LaunchServerAsync(LspInstallResult result, ITaskStatusNotifier taskNotifier)
        {
            async Task LaunchAsync()
            {
                taskNotifier.ProgressText = "Launching Language Server...";
                await StartAsync.InvokeAsync(this, EventArgs.Empty);
            }

            void RecordInitialize(ITelemetryLogger telemetryLogger, TaskResult taskResult, long milliseconds)
            {
                var args = LspInstallUtil.CreateRecordLspInstallerArgs(result, milliseconds);
                args.Id = LanguageServerIdentifier;
                telemetryLogger.RecordSetupInitialize(taskResult, args);
            }

            await _toolkitContext.TelemetryLogger.InvokeTimeAndRecordAsync(LaunchAsync, RecordInitialize);
        }

        /// <summary>
        /// Configures the language client with handlers for custom messages originating from the language server
        /// </summary>
        private void SetupLspMessageHandler()
        {
            var messageHandler = CreateLspMessageHandler();
            messageHandler.WorkspaceConfigurationAsync += OnRequestWorkspaceConfigurationAsync;
            messageHandler.TelemetryEvent += OnTelemetryEventNotification;
            messageHandler.ConnectionMetadataAsync += OnRequestConnectionMetadataAsync;
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


        /// <summary>
        /// Handles when the language server sends a notification for a telemetry event and we want to emit it
        /// The contents of <see cref="TelemetryEventArgs.MetricEvent"/>
        /// are emitted to telemetry backend as this handler completes.
        /// </summary>
        private void OnTelemetryEventNotification(object sender, TelemetryEventArgs args)
        {
            TelemetryEventNotification?.Invoke(this, args);
        }

        /// <summary>
        /// Handles when the language server sends a request for the auth connection information, and
        /// we want to send a response back. The contents of <see cref="ConnectionMetadataEventArgs.Response"/>
        /// are sent after this handler completes.
        /// </summary>
        private async Task OnRequestConnectionMetadataAsync(object sender, ConnectionMetadataEventArgs args)
        {
            var asyncHandler = RequestConnectionMetadataAsync;
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
                messageHandler.TelemetryEvent -= OnTelemetryEventNotification;
                messageHandler.ConnectionMetadataAsync -= OnRequestConnectionMetadataAsync;
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
            try
            {
                _toolkitContext.ToolkitHost.OutputToHostConsoleAsync($"Activating: {Name}", false).LogExceptionAndForget();
                _processWatcher?.Dispose();
                Status = LspClientStatus.SettingUp;

                await Task.Yield();

                await TaskScheduler.Default;

                var serverProcess = CreateLspProcess();
                _processWatcher = new ProcessWatcher(serverProcess);
                _processWatcher.ProcessEnded += OnProcessEnded;

                _logger.Info($"Launching language server: {Name}");
                if (!serverProcess.Start())
                {
                    Status = LspClientStatus.Error;

                    // null indicates the server cannot be started
                    return null;
                }

                await OnBeforeLspConnectionStartsAsync(serverProcess);

                return new Connection(serverProcess.StandardOutput.BaseStream, serverProcess.StandardInput.BaseStream);
            }
            catch (Exception e)
            {
                Status = LspClientStatus.Error;
                _processWatcher?.Dispose();

                _logger.Error($"Failed to launch language server {Name}", e);

                throw;
            }
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


            ApplyProxySettings(info);

            var process = new Process
            {
                StartInfo = info,
            };  

            return process;
        }

        /// <summary>
        /// Applies proxy settings if configured to LSP server
        /// </summary>
        /// <param name="info"></param>
        private void ApplyProxySettings(ProcessStartInfo info)
        {
            var proxySettings = _proxySettingsRepository.Get();
            var proxyUrl = proxySettings.GetProxyUrl();
            if (!string.IsNullOrWhiteSpace(proxyUrl))
            {
                // sets proxy env variable for the node based LSP server as per https://docs.aws.amazon.com/sdk-for-javascript/v2/developer-guide/node-configuring-proxies.html
                // LSP server then applies this proxy config to the AWS SDK 
                info.EnvironmentVariables["HTTPS_PROXY"] = proxyUrl;
            }
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
        /// Raised when the language server process has terminated.
        /// </summary>
        private void OnProcessEnded(object sender, EventArgs e)
        {
            // When the process unexpectedly terminates, we want the rest of the system to stop operating as it
            // the server is available. Examples include:
            // - the language server crashes
            // - a process or user on the host system killed the process
            // When the user closes an open folder or solution, that will also cause Visual Studio to
            // end the language server process by sending 'shutdown' and 'exit' messages over LSP.
            // We cannot differentiate between these two scenarios, but this is okay because if the user
            // were to load another solution, Visual Studio will go through the language client activation process,
            // which will advance the Status to SettingUp, and then Running.
            Status = LspClientStatus.Error;
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
            _toolkitContext.ToolkitHost.OutputToHostConsoleAsync($"Initialized: {Name}", false).LogExceptionAndForget();
            Status = LspClientStatus.Running;

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
            Status = LspClientStatus.Error;

            _toolkitContext.ToolkitHost.OutputToHostConsoleAsync($"Failed to initialize Language Server: {Name}", false).LogExceptionAndForget();
            _toolkitContext.ToolkitHost.OutputToHostConsoleAsync($"- Status: {initializationState.Status}", false).LogExceptionAndForget();
            _toolkitContext.ToolkitHost.OutputToHostConsoleAsync($"- Status Message: {initializationState.StatusMessage}", false).LogExceptionAndForget();
            _toolkitContext.ToolkitHost.OutputToHostConsoleAsync($"- Exception: {initializationState.InitializationException?.Message}", false).LogExceptionAndForget();

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

        private void RaiseStatusChanged(LspClientStatus clientStatus)
        {
            StatusChanged?.Invoke(this, new LspClientStatusChangedEventArgs(clientStatus));
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
//             _rpc.LogJsonRpcMessages(this, _toolkitContext.ToolkitHost);
// #endif

            return Task.CompletedTask;
        }

        #endregion
    }
}
