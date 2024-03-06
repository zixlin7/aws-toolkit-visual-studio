using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Tasks;
using Amazon.AWSToolkit.Util;

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

        public async Task PauseAutoSuggestAsync()
        {
            var settings = await _settingsRepository.GetAsync();
            settings.AutomaticSuggestionsEnabled = false;
            _settingsRepository.Save(settings);
            OutputStatus("CodeWhisperer's automatic suggestions have been paused.");
        }

        public async Task ResumeAutoSuggestAsync()
        {
            var settings = await _settingsRepository.GetAsync();
            settings.AutomaticSuggestionsEnabled = true;
            _settingsRepository.Save(settings);
            OutputStatus("CodeWhisperer's automatic suggestions will be displayed.");
        }

        public async Task<bool> IsAutoSuggestPausedAsync()
        {
            var settings = await _settingsRepository.GetAsync();
            return !settings.AutomaticSuggestionsEnabled;
        }

        public event EventHandler<PauseStateChangedEventArgs> PauseAutoSuggestChanged;

        public async Task<SuggestionSession> GetSuggestionsAsync(GetSuggestionsRequest request)
        {
            var suggestionSession = new SuggestionSession();

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

                suggestionSession.RequestedAtEpoch = DateTime.Now.AsUnixMilliseconds();

                var response = await inlineCompletions.GetInlineCompletionsAsync(inlineCompletionRequest);

                suggestionSession.SessionId = response.SessionId;
                suggestionSession.Suggestions.AddAll(response.Items.Select(AsSuggestion));
            }
            catch (Exception e)
            {
                _logger.Error("Failure getting suggestions from language server", e);
                _toolkitContextProvider.GetToolkitContext().ToolkitHost.OutputToHostConsoleAsync($"AWS Toolkit was unable to get CodeWhisperer suggestions: {e.Message}", false).LogExceptionAndForget();
            }

            return suggestionSession;
        }

        private Suggestion AsSuggestion(InlineCompletionItem inlineCompletion)
        {
            var references = inlineCompletion.References?.Select(reference =>
            {
                var data = new SuggestionReference()
                {
                    Name = reference.ReferenceName,
                    LicenseName = reference.LicenseName,
                    Url = reference.ReferenceUrl,
                    // Assume the full suggestion text is attributed if position details are missing
                    StartIndex = reference.Position?.StartCharacter ?? 0,
                    EndIndex = reference.Position?.EndCharacter ?? inlineCompletion.InsertText.Length,
                };

                // Prevent out of boundary access issues
                data.StartIndex = Math.Max(data.StartIndex, 0);
                data.EndIndex = Math.Min(data.EndIndex, inlineCompletion.InsertText.Length);

                return data;
            });

            return new Suggestion()
            {
                Text = inlineCompletion.InsertText,
                ReplacementRange = inlineCompletion.Range.AsToolkitRange(),
                Id = inlineCompletion.ItemId,
                References = references?.ToList(),
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
                IsPaused = !e.Settings.AutomaticSuggestionsEnabled,
            });
        }

        private void OutputStatus(string message)
        {
            _toolkitContextProvider.GetToolkitContext().ToolkitHost.OutputToHostConsole(message, true);
        }
    }
}
