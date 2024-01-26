using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.InlineCompletions;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.SecurityScans;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.InlineCompletions;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.SecurityScans;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Suggestions;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Clients
{
    public class FakeCodeWhispererClient : FakeToolkitLspClient, ICodeWhispererLspClient
    {
        public readonly FakeSuggestionSessionResultsPublisher SuggestionSessionResultsPublisher =
            new FakeSuggestionSessionResultsPublisher();

        public FakeInlineCompletions InlineCompletions { get; } = new FakeInlineCompletions();
        public FakeSecurityScans SecurityScans { get; } = new FakeSecurityScans();

        public IInlineCompletions CreateInlineCompletions()
        {
            return InlineCompletions;
        }

        public ISuggestionSessionResultsPublisher CreateSessionResultsPublisher()
        {
            return SuggestionSessionResultsPublisher;
        }

        public ISecurityScans CreateSecurityScan()
        {
            return SecurityScans;
        }
    }
}
