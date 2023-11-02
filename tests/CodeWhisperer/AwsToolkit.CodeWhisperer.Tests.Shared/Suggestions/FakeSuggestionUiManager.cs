using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions
{
    internal class FakeSuggestionUiManager : ISuggestionUiManager
    {
        public Task ShowAsync(IEnumerable<Suggestion> suggestions, ICodeWhispererTextView textView)
        {
            throw new NotImplementedException();
        }
    }
}
