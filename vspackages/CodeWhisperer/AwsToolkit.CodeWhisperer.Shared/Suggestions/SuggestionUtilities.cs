using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions
{
    public static class SuggestionUtilities
    {
        public static SuggestionInvocationProperties CreateInvocationProperties(bool isAutoSuggestion, string sessionId, int requestPosition, long requestedAtEpoch)
        {
            return new SuggestionInvocationProperties()
            {
                TriggerKind = isAutoSuggestion
                    ? InlineCompletionTriggerKind.Automatic
                    : InlineCompletionTriggerKind.Invoke,
                SessionId = sessionId,
                RequestPosition = requestPosition,
                RequestedAtEpoch = requestedAtEpoch
            };
        }
    }
}
