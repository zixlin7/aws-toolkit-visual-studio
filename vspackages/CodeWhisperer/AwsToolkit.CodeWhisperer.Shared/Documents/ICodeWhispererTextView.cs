using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AWSToolkit.Models.Text;

using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Text.Editor;

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
        Task<Position> GetDocumentPositionAsync(int position);

        /// <summary>
        /// Gets the current cursor position within a text view.
        /// </summary>
        Position GetCursorPosition();

        /// <summary>
        /// Gets the text between two positions within a text view
        /// </summary>
        Task<string> GetTextBetweenPositionsAsync(int startPosition, int endPosition);

        /// <summary>
        /// Gets the IWpfTextView.
        /// </summary>
        /// <returns></returns>
        IWpfTextView GetWpfTextView();

        /// <summary>
        /// Creates the Get Suggestions Request used by Visual Studio to display proposals from suggestions.
        /// </summary>
        GetSuggestionsRequest CreateGetSuggestionsRequest(bool isAutoSuggestion);

        /// <summary>
        /// Creates the proposal used by Visual Studio to display a suggestion
        /// </summary>
        Proposal CreateProposal(string replacementText, string description, string id);
    }
}
