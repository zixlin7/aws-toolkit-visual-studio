using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Configuration;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

namespace Amazon.AwsToolkit.CodeWhisperer.Settings
{
    /// <summary>
    /// Core (not tied to a specific service) implementation that can be used by MEF components responsible for pushing configuration state
    /// to language servers.
    ///
    /// Each service should derive this class, and implement the hooks to retrieve configuration blobs.
    /// The implementations should then be MEF imported in a manner that allows them to self-register with
    /// the language client's events.
    /// </summary>
    internal abstract class SettingsPublisher : IDisposable
    {
        private readonly IToolkitLspClient _lspClient;
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;
        private ILspConfiguration _lspConfiguration;

        [ImportingConstructor]
        protected SettingsPublisher(IToolkitLspClient lspClient, ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _lspClient = lspClient;
            _taskFactoryProvider = taskFactoryProvider;

            _lspClient.InitializedAsync += OnLspClientInitializedAsync;
            _lspClient.RequestWorkspaceConfigurationAsync += OnLspClientRequestWorkspaceConfigurationAsync;
        }

        /// <summary>
        /// Pushes the current configuration state to the language server
        /// through the "workspace/didChangeConfigurations" message
        /// </summary>
        public async Task RaiseDidChangeConfigurationAsync()
        {
            if (_lspConfiguration == null || _lspClient.Status != LspClientStatus.Running)
            {
                return;
            }

            // DEXP contract: send an empty payload to the server. The server will then
            // send a request back to the client to fetch the state.
            // This is based on current lsp protocol guidance.
            await _lspConfiguration.RaiseDidChangeConfigurationAsync(new object());
        }

        /// <summary>
        /// Pushes the current configuration state once the language client has been initialized.
        /// </summary>
        private async Task OnLspClientInitializedAsync(object sender, EventArgs e)
        {
            _lspConfiguration = _lspClient.CreateLspConfiguration();

            await RaiseDidChangeConfigurationAsync();
        }

        /// <summary>
        /// Handles when the language server has requested the configuration state through "workspace/configuration"
        /// The configuration state is retrieved, and provided back to the event instigator.
        /// </summary>
        private async Task OnLspClientRequestWorkspaceConfigurationAsync(object sender, WorkspaceConfigurationEventArgs e)
        {
            await LoadConfigurationStateAsync(e.Response);
        }

        /// <summary>
        /// Hook for service implementations to load their configuration state
        /// </summary>
        /// <param name="configurationState">Populate with configuration key-value pairings to be sent to the language server</param>
        internal abstract Task LoadConfigurationStateAsync(Dictionary<string, object> configurationState);

        /// <summary>
        /// Standardized handler that service implementations can use to push the configuration state to
        /// the language server whenever settings are saved.
        /// </summary>
        protected virtual void OnSettingsRepositorySaved(object sender, EventArgs e)
        {
            _taskFactoryProvider.JoinableTaskFactory.Run(async () => await RaiseDidChangeConfigurationAsync());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lspClient.RequestWorkspaceConfigurationAsync -= OnLspClientRequestWorkspaceConfigurationAsync;
                _lspClient.InitializedAsync -= OnLspClientInitializedAsync;
            }
        }
    }
}
