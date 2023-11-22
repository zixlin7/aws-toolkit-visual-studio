using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models
{
    public class SuggestionInvocationProperties
    {
        /// <summary>
        /// The inline completion trigger kind (i.e. Manual / Automatic)
        /// </summary>
        public InlineCompletionTriggerKind TriggerKind { get; set; }

        /// <summary>
        /// The LSP session id for the suggestion
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// The caret position of the original inline completions request
        /// </summary>
        public int RequestPosition { get; set; }

        /// <summary>
        /// The epoch time(in milliseconds) when the inline completions request started
        /// </summary>
        public long RequestedAtEpoch { get; set; }
    }
}
