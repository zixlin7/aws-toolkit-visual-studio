using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.InlineCompletions;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.InlineCompletions;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Clients
{
    public class FakeCodeWhispererClient : FakeToolkitLspClient, ICodeWhispererLspClient
    {
        public FakeInlineCompletions InlineCompletions { get; } = new FakeInlineCompletions();

        public IInlineCompletions CreateInlineCompletions()
        {
            return InlineCompletions;
        }
    }
}
