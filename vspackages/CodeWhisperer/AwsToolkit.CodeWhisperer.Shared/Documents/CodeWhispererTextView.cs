using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    public interface ICodeWhispererTextView
    {
        /// <summary>
        /// The full path of the document represented by this text view
        /// </summary>
        string GetFilePath();

        /// <summary>
        /// Converts the given absolute position in a text view
        /// to the line and character index within the document.
        /// </summary>
        Position GetDocumentPosition(int position);

        /// <summary>
        /// Creates the proposal used by Visual Studio to display a suggestion
        /// </summary>
        Proposal CreateProposal(string replacementText, string description);
    }

    /// <summary>
    /// Provides an abstraction layer around Visual Studio TextView types, allowing
    /// us to add test coverage to more integration logic without requiring complicated fakes for VS types.
    /// </summary>
    internal class CodeWhispererTextView : ICodeWhispererTextView
    {
        private readonly IWpfTextView _wpfTextView;
        private readonly IVsTextView _vsTextView;
        private string _filePath;

        protected CodeWhispererTextView(IWpfTextView wpfTextView, IVsTextView vsTextView)
        {
            _wpfTextView = wpfTextView;
            _vsTextView = vsTextView;
        }

        public static async Task<CodeWhispererTextView> CreateAsync(IWpfTextView wpfTextView)
        {
            var vsTextView = await wpfTextView.ToIVsTextViewAsync();
            return new CodeWhispererTextView(wpfTextView, vsTextView);
        }

        public string GetFilePath()
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                _filePath = _wpfTextView.GetFilePath();
            }

            return _filePath;
        }

        public Position GetDocumentPosition(int position)
        {
            return _vsTextView.GetDocumentPosition(position);
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
