using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

namespace Amazon.AwsToolkit.CodeWhisperer.Settings
{
    /// <summary>
    /// Handles the overall Enabled/Disabled state for CodeWhisperer, based on the Configuration value.
    /// </summary>
    [Export(typeof(EnabledStateManager))]
    internal class EnabledStateManager : IDisposable
    {
        private readonly ICodeWhispererLspClient _lspClient;
        private readonly ICodeWhispererSettingsRepository _settingsRepository;
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;

        [ImportingConstructor]
        public EnabledStateManager(
            ICodeWhispererLspClient lspClient,
            ICodeWhispererSettingsRepository settingsRepository,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _lspClient = lspClient;
            _settingsRepository = settingsRepository;
            _taskFactoryProvider = taskFactoryProvider;

            _settingsRepository.SettingsSaved += OnSettingsRepositorySaved;
        }

        protected virtual void OnSettingsRepositorySaved(object sender, CodeWhispererSettingsSavedEventArgs e)
        {
            _taskFactoryProvider.JoinableTaskFactory.Run(async () => await OnSettingsRepositorySavedAsync(e));
        }

        /// <summary>
        /// Start or stop the language client if appropriate
        /// </summary>
        protected async Task OnSettingsRepositorySavedAsync(CodeWhispererSettingsSavedEventArgs e)
        {
            if (e.Settings.IsEnabled && CanStart())
            {
                await _lspClient.StartClientAsync();
            }
            else if (!e.Settings.IsEnabled && CanStop())
            {
                await _lspClient.StopClientAsync();
            }
        }

        private bool CanStart()
        {
            return _lspClient.Status != LspClientStatus.Running;
        }

        private bool CanStop()
        {
            return _lspClient.Status == LspClientStatus.SettingUp || _lspClient.Status == LspClientStatus.Running;
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
                _settingsRepository.SettingsSaved -= OnSettingsRepositorySaved;
            }
        }
    }
}
