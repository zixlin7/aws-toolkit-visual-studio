using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

using Microsoft.VisualStudio.Text.Editor;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions
{
    internal class FakeSuggestionUiManager : ISuggestionUiManager
    {
        public bool IsSuggestionDisplayed(ICodeWhispererTextView textView)
        {
            throw new NotImplementedException();
        }

        public Task ShowAsync(IEnumerable<Suggestion> suggestions, SuggestionInvocationProperties invocationProperties,
            ICodeWhispererTextView view)
        {
            throw new NotImplementedException();
        }
    }
}
