using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

namespace Amazon.AwsToolkit.CodeWhisperer.Settings
{
    public static class CodeWhispererSettingsNames
    {
        public const string IncludeSuggestionsWithCodeReferences = "includeSuggestionsWithCodeReferences";
    }

    /// <summary>
    /// MEF Component interface responsible for pushing configuration state to the language server.
    /// The publisher's implementation is largely internal, but this interface provides us with
    /// a clean stub point where needed.
    /// </summary>
    public interface ICodeWhispererSettingsPublisher
    {
    }

    /// <summary>
    /// CodeWhisperer MEF component responsible for pushing configuration state to the language server.
    /// </summary>
    [Export(typeof(ICodeWhispererSettingsPublisher))]
    internal class CodeWhispererSettingsPublisher : SettingsPublisher, ICodeWhispererSettingsPublisher
    {
        private readonly ICodeWhispererSettingsRepository _settingsRepository;

        [ImportingConstructor]
        public CodeWhispererSettingsPublisher(ICodeWhispererSettingsRepository settingsRepository,
            ICodeWhispererLspClient lspClient, ToolkitJoinableTaskFactoryProvider taskFactoryProvider) : base(lspClient,
            taskFactoryProvider)
        {
            _settingsRepository = settingsRepository;

            _settingsRepository.SettingsSaved += OnSettingsRepositorySaved;
        }

        /// <summary>
        /// Retrieves the CodeWhisperer configuration state that the language server is interested in.
        /// </summary>
        internal override async Task LoadConfigurationStateAsync(Dictionary<string, object> configurationState)
        {
            var settings = await _settingsRepository.GetAsync();

            configurationState[CodeWhispererSettingsNames.IncludeSuggestionsWithCodeReferences] = settings.IncludeSuggestionsWithReferences;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _settingsRepository.SettingsSaved -= OnSettingsRepositorySaved;
            }

            base.Dispose(disposing);
        }
    }
}
