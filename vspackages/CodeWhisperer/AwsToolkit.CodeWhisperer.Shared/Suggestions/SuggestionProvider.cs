using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AWSToolkit.Context;

using log4net;

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions
{
    [Export(typeof(ISuggestionProvider))]
    internal class SuggestionProvider : ISuggestionProvider
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SuggestionProvider));

        private readonly ICodeWhispererLspClient _lspClient;
        private readonly ICodeWhispererSettingsRepository _settingsRepository;
        private readonly IToolkitContextProvider _toolkitContextProvider;

        [ImportingConstructor]
        public SuggestionProvider(
            ICodeWhispererLspClient lspClient,
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

        public async Task<IEnumerable<Suggestion>> GetSuggestionsAsync(GetSuggestionsRequest request)
        {
            var suggestions = new List<Suggestion>();

            try
            {
                var inlineCompletions = _lspClient.CreateInlineCompletions();

                var inlineCompletionRequest = new InlineCompletionParams()
                {
                    TextDocument = new TextDocumentIdentifier() { Uri = new Uri(request.FilePath), },
                    Context = new InlineCompletionContext()
                    {
                        TriggerKind = request.IsAutoSuggestion
                            ? InlineCompletionTriggerKind.Automatic
                            : InlineCompletionTriggerKind.Invoke,
                    },
                    Position = request.CursorPosition.AsLspPosition(),
                };

                var response = await inlineCompletions.GetInlineCompletionsAsync(inlineCompletionRequest);

                suggestions.AddRange(response.Items.Select(AsSuggestion));
            }
            catch (Exception e)
            {
                _logger.Error("Failure getting suggestions from language server", e);
                _toolkitContextProvider.GetToolkitContext().ToolkitHost.OutputToHostConsole($"AWS Toolkit was unable to get CodeWhisperer suggestions: {e.Message}", false);
            }

            return suggestions;
        }

        private Suggestion AsSuggestion(InlineCompletionItem inlineCompletion)
        {
            return new Suggestion()
            {
                Text = inlineCompletion.InsertText,
                ReplacementRange = inlineCompletion.Range.AsToolkitRange(),
            };
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
