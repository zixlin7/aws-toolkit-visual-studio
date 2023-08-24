using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials;
using Amazon.AWSToolkit.Context;

using log4net;

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
    public abstract class ToolkitLspClient : ILanguageClient
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ToolkitLspClient));

        protected readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };

        [Import]
        protected IToolkitContextProvider _toolkitContextProvider;
        protected ToolkitContext _toolkitContext => _toolkitContextProvider.GetToolkitContext();

        /// <summary>
        /// Implementations should set this to true if they want this class to orchestrate Credentials initialization.
        /// </summary>
        protected readonly bool _initializeServerWithCredentials = false;

        public ToolkitLspClient(bool initializeServerWithCredentials = false)
        {
            _initializeServerWithCredentials = initializeServerWithCredentials;
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
        public virtual object InitializationOptions { get; } = new {};

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

            // TODO : Determine the latest version of the language server, and download+validate+store it if necessary
            // TODO : Wrap the download in ITaskStatusNotifier

            // TODO : Have a separate controller responsible for starting the language server
            // (move the line below; call it elsewhere once we have a place in the UI that users can "turn it on and off")
            await StartAsync.InvokeAsync(this, EventArgs.Empty);
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

            var serverProcess = await CreateLspProcessAsync();

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
        private async Task<Process> CreateLspProcessAsync()
        {
            var info = new ProcessStartInfo
            {
                WorkingDirectory = await GetServerWorkingDirAsync(),
                FileName = await GetServerPathAsync(),
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
        /// The folder the language server process should be started from
        /// </summary>
        protected abstract Task<string> GetServerWorkingDirAsync();

        /// <summary>
        /// The command to launch the language server
        /// </summary>
        /// <returns></returns>
        protected abstract Task<string> GetServerPathAsync();

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
                args.Add("--pre-init-encryption");
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
            var message = new CredentialsEncryptionInitialization()
            {
                Mode = CredentialsEncryptionInitialization.Modes.Jwt,
                Key = "FOO", // TODO : Implement when setting up Auth
            };

            var json = JsonConvert.SerializeObject(message, _jsonSerializerSettings);

            writer.WriteLine(json);
        }

        /// <summary>
        /// VS calls this after successfully making initialization calls with the language server
        /// </summary>
        /// <inheritdoc cref="ILanguageClient.OnServerInitializedAsync"/>
        public Task OnServerInitializedAsync()
        {
            _logger.Info($"Language server initialization handshake completed: {Name}");
            _toolkitContext.ToolkitHost.OutputToHostConsole($"Initialized: {Name}");
            return Task.CompletedTask;
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
    }
}
