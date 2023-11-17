using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Suggestions;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Suggestions
{
    public class FakeSuggestionSessionResultsPublisher : ISuggestionSessionResultsPublisher
    {
        public LogInlineCompletionSessionResultsParams SessionResultsParam =
            new LogInlineCompletionSessionResultsParams();

        public Task SendInlineCompletionSessionResultAsync(LogInlineCompletionSessionResultsParams result)
        {
            SessionResultsParam = result;
            return Task.CompletedTask;
        }
    }
}
