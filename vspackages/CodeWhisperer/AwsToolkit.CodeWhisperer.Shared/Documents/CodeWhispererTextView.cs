using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AwsToolkit.VsSdk.Common.Documents;
using Amazon.AWSToolkit.Models.Text;

using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Amazon.AwsToolkit.CodeWhisperer.Documents
{
    /// <summary>
    /// Provides an abstraction layer around Visual Studio TextView types, allowing
    /// us to add test coverage to more integration logic without requiring complicated fakes for VS types.
    /// </summary>
    internal class CodeWhispererTextView : ICodeWhispererTextView
    {
        private readonly IWpfTextView _wpfTextView;
        private IVsTextView _vsTextView;
        private string _filePath;

        internal CodeWhispererTextView(IWpfTextView wpfTextView)
        {
            _wpfTextView = wpfTextView;
        }

        public string GetFilePath()
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                _filePath = _wpfTextView.GetFilePath();
            }

            return _filePath;
        }

        public async Task<Position> GetDocumentPositionAsync(int position)
        {
            if (_vsTextView == null)
            {
                _vsTextView = await _wpfTextView.ToIVsTextViewAsync();
            }

            return _vsTextView.GetDocumentPosition(position);
        }

        public Position GetCursorPosition()
        {
            return _wpfTextView.GetCursorPosition();
        }

        public IWpfTextView GetWpfTextView()
        {
            return _wpfTextView;
        }

        public GetSuggestionsRequest CreateGetSuggestionsRequest(bool isAutoSuggestion)
        {
            return new GetSuggestionsRequest()
            {
                FilePath = GetFilePath(),
                CursorPosition = GetCursorPosition(),
                IsAutoSuggestion = isAutoSuggestion
            };
        }

        public Proposal CreateProposal(string replacementText, string description)
        {
            return new Proposal(description, CreateProposedEdits(replacementText).ToList(),
                _wpfTextView.Caret.Position.VirtualBufferPosition, null,
                ProposalFlags.ShowCommitHighlight);
        }

        private IEnumerable<ProposedEdit> CreateProposedEdits(string replacementText)
        {
            // We want to adjust the code at all current cursor locations
            return _wpfTextView.GetMultiSelectionBroker()
                .AllSelections
                .Select(selection =>
                    new ProposedEdit(selection.Extent.SnapshotSpan, replacementText, null));
        }
    }
}
