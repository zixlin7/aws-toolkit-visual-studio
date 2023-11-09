using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AWSToolkit.Models.Text;

using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Documents
{
    public class FakeCodeWhispererTextView : ICodeWhispererTextView
    {
        public string FilePath;

        public string GetFilePath()
        {
            return FilePath;
        }

        public Task<Position> GetDocumentPositionAsync(int position)
        {
            return Task.FromResult(new Position(0, position));
        }

        public Position GetCursorPosition()
        {
            return new Position(0, 0);
        }

        public Task<string> GetTextBetweenPositionsAsync(int startPosition, int endPosition)
        {
            throw new System.NotImplementedException();
        }

        public IWpfTextView GetWpfTextView()
        {
            throw new System.NotImplementedException();
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
            var edit = new ProposedEdit(new SnapshotSpan(), replacementText, null);
            var caretPosition = new VirtualSnapshotPoint(new SnapshotPoint());

            return new Proposal(description, new List<ProposedEdit>() { edit }, caretPosition);
        }
    }
}
