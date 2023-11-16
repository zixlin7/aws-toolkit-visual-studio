using System;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AWSToolkit.CommonUI;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AWSToolkit.Tasks;
using Amazon.AwsToolkit.VsSdk.Common.Documents;
using Microsoft.VisualStudio.Threading;
using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Margins
{
    /// <summary>
    /// Listens for text document changes and triggers inline suggestions based on user keystrokes
    /// </summary>
    public class CodeWhispererDocumentViewModel : BaseModel, IDisposable
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CodeWhispererDocumentViewModel));

        private readonly ICodeWhispererTextView _textView;
        private readonly ICodeWhispererManager _manager;
        private readonly ISuggestionUiManager _suggestionUiManager;
        private List<Suggestion> _requestedSuggestions = null;

        // Checks if we are currently requesting a suggestion and attempting to display it.
        // This ensures that we are only handling one request at a time
        private bool _isAttemptingToDisplaySuggestion;

        public CodeWhispererDocumentViewModel(
            ICodeWhispererTextView textView,
            ICodeWhispererManager manager,
            ISuggestionUiManager suggestionUiManager)
        {
            _textView = textView;
            _manager = manager;
            _suggestionUiManager = suggestionUiManager;

            _textView.GetWpfTextView().TextBuffer.Changed += OnTextChanged;
        }

        private void OnTextChanged(object sender, TextContentChangedEventArgs e)
        {
            if (_isAttemptingToDisplaySuggestion)
            {
                return;
            }

            DisplaySuggestionAsync(e).LogExceptionAndForget();
        }

        private async Task DisplaySuggestionAsync(TextContentChangedEventArgs e)
        {
            await TaskScheduler.Default;

            _isAttemptingToDisplaySuggestion = true;

            try
            {
                if (_manager.ClientStatus != LspClientStatus.Running
                    || _manager.ConnectionStatus != ConnectionStatus.Connected
                    || await _manager.IsAutoSuggestPausedAsync()
                    || IsSuggestionDisplayed()
                    || !IsUserEdit(e.Changes.LastOrDefault()))
                {
                    return;
                }

                var request = _textView.CreateGetSuggestionsRequest(true);
                var requestPosition = _textView.GetWpfTextView().GetCaretSnapshotPosition();

                var suggestionSession = await _manager.GetSuggestionsAsync(request);

                _requestedSuggestions = suggestionSession.Suggestions.ToList();

                // only if suggestion session is valid, attempt to load suggestions in UI
                if (suggestionSession.IsValid())
                {
                    var invocationProperties =
                        SuggestionUtilities.CreateInvocationProperties(request.IsAutoSuggestion, suggestionSession.SessionId, requestPosition, suggestionSession.RequestedAtEpoch);
                    await _suggestionUiManager.ShowAsync(_requestedSuggestions, invocationProperties, _textView);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to display CodeWhisperer auto-suggestion", ex);
            }
            finally
            {
                _isAttemptingToDisplaySuggestion = false;
            }
        }

        /// <summary>
        /// Checks if there is a suggestion being displayed in the UI
        /// </summary>
        private bool IsSuggestionDisplayed()
        {
            return _suggestionUiManager.IsSuggestionDisplayed(_textView);
        }

        /// <summary>
        /// Checks if the change was made by a user keystroke or direct edit.
        /// Returns false for automated edits, re-factors, off-cursor/off-screen file manipulation, etc.
        /// </summary>
        private bool IsUserEdit(ITextChange change)
        {
            // TODO : IDE-11992 - Handle all keystroke triggers
            return IsNewLine(change) || IsValidChar(change);
        }

        private static bool IsNewLine(ITextChange change)
        {
            return change.NewText == Environment.NewLine;
        }

        private static bool IsValidChar(ITextChange change)
        {
            return change.NewText.Length == 1
                    && (char.IsLetterOrDigit(change.NewText[0])
                        || char.IsSeparator(change.NewText[0])
                        || char.IsPunctuation(change.NewText[0]));
        }

        public void Dispose()
        {
            _textView.GetWpfTextView().TextBuffer.Changed -= OnTextChanged;
        }
    }
}
