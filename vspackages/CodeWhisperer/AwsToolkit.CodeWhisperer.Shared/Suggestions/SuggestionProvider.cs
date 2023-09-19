using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions
{
    [Export(typeof(ISuggestionProvider))]
    internal class SuggestionProvider : ISuggestionProvider
    {
        // TODO : IDE-11522 : create and use a CodeWhispererLspClient instead of the generalized ToolkitLspClient
        private readonly IToolkitLspClient _lspClient;
        private readonly ICodeWhispererSettingsRepository _settingsRepository;
        private readonly IToolkitContextProvider _toolkitContextProvider;

        [ImportingConstructor]
        public SuggestionProvider(
            IToolkitLspClient lspClient,
            ICodeWhispererSettingsRepository settingsRepository,
            IToolkitContextProvider toolkitContextProvider)
        {
            _lspClient = lspClient;
            _settingsRepository = settingsRepository;
            _toolkitContextProvider = toolkitContextProvider;

            _settingsRepository.SettingsSaved += SettingsRepositoryOnSettingsSaved;
        }

        public Task PauseAutoSuggestAsync()
        {
            throw new NotImplementedException();
        }

        public Task ResumeAutoSuggestAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsAutoSuggestPausedAsync()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<PauseStateChangedEventArgs> PauseAutoSuggestChanged;

        public Task<IEnumerable<Suggestion>> GetSuggestionsAsync() // TODO : IDE-11522 : define the request model
        {
            // todo : IDE-11522 : get proxy from _lspClient and make a "get suggestions" request
            return Task.FromResult(Enumerable.Empty<Suggestion>());
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
                _settingsRepository.SettingsSaved -= SettingsRepositoryOnSettingsSaved;
            }
        }

        private void SettingsRepositoryOnSettingsSaved(object sender, CodeWhispererSettingsSavedEventArgs e)
        {
            // todo : IDE-11364 : raise PauseAutoSuggestChanged when paused state changes
            // Until then, always raise the event, so that we don't have a compiler warning for PauseAutoSuggestChanged being unused
            PauseAutoSuggestChanged?.Invoke(this, new PauseStateChangedEventArgs()
            {
                IsPaused = e.Settings.PauseAutomaticSuggestions,
            });
        }
    }
}
