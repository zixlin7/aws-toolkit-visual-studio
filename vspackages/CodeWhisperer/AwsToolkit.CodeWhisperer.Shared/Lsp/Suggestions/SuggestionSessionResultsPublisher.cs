using System.Threading.Tasks;

using StreamJsonRpc;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Suggestions
{
    internal static class MessageNames
    {
        /// <summary>
        /// Pushed by the Toolkit to the language server to signal that
        /// suggestion session completion result is available
        /// </summary>
        public const string LogInlineCompletionSessionResult = "aws/logInlineCompletionSessionResults";
    }

    /// <summary>
    /// Abstraction for suggestion completion session related communications with the language server
    /// </summary>
    public interface ISuggestionSessionResultsPublisher
    {
        Task SendInlineCompletionSessionResultAsync(LogInlineCompletionSessionResultsParams result);
    }

    /// <summary>
    /// Suggestion completion session related communications with the language server.
    /// </summary>
    public class SuggestionSessionResultsPublisher : ISuggestionSessionResultsPublisher
    {
        private readonly JsonRpc _rpc;

        public SuggestionSessionResultsPublisher(JsonRpc rpc)
        {
            _rpc = rpc;
        }

        public async Task SendInlineCompletionSessionResultAsync(LogInlineCompletionSessionResultsParams result)
        {
            await _rpc.NotifyWithParameterObjectAsync(MessageNames.LogInlineCompletionSessionResult, result);
        }
    }
}
