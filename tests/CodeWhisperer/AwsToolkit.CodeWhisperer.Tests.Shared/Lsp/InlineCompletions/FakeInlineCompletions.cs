using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.InlineCompletions;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.InlineCompletions
{
    public class FakeInlineCompletions : IInlineCompletions
    {
        public InlineCompletionList InlineCompletions { get; set; } = new InlineCompletionList();

        public Task<InlineCompletionList> GetInlineCompletionsAsync(InlineCompletionParams inlineCompletionParams)
        {
            return Task.FromResult(InlineCompletions);
        }
    }
}
