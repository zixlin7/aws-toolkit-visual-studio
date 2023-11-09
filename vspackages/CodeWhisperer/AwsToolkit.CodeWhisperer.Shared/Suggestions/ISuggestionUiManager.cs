using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions
{
    /// <summary>
    /// MEF Component used to manage the UI for displaying inline suggestions.
    /// </summary>
    public interface ISuggestionUiManager
    {
        bool IsSuggestionDisplayed(ICodeWhispererTextView textView);

        Task ShowAsync(IEnumerable<Suggestion> suggestions, SuggestionInvocationProperties invocationProperties, ICodeWhispererTextView view);
    }
}
