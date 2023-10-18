using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;

using StreamJsonRpc;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.InlineCompletions
{
    internal static class MessageNames
    {
        public const string GetInlineCompletions = "aws/textDocument/inlineCompletionWithReferences";
    }

    /// <summary>
    /// Abstraction for inline completions related communications with the language server
    /// </summary>
    public interface IInlineCompletions
    {
        Task<InlineCompletionList> GetInlineCompletionsAsync(InlineCompletionParams inlineCompletionParams);
    }

    /// <summary>
    /// Inline completions related communications with the language server.
    /// Some messages use JsonRpc directly instead of JsonRpc proxies because the
    /// JsonRpc proxy does not emit named parameters the way the language server is expecting them.
    /// </summary>
    public class InlineCompletions : IInlineCompletions
    {
        private readonly JsonRpc _rpc;

        public InlineCompletions(JsonRpc rpc)
        {
            _rpc = rpc;
        }

        public async Task<InlineCompletionList> GetInlineCompletionsAsync(InlineCompletionParams inlineCompletionParams)
        {
            return await _rpc.InvokeWithParameterObjectAsync<InlineCompletionList>(
                MessageNames.GetInlineCompletions, inlineCompletionParams);
        }
    }
}
