using System.Collections.Generic;

using Newtonsoft.Json;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Suggestions
{
    public class LogInlineCompletionSessionResultsParams
    {
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        [JsonProperty("totalSessionDisplayTime")]
        public long? TotalSessionDisplayTime { get; set; }

        [JsonProperty("firstCompletionDisplayLatency")]
        public long? FirstCompletionDisplayLatency { get; set; }

        [JsonProperty("typeaheadLength")]
        public int? TypeaheadLength { get; set; }

        [JsonProperty("completionSessionResult")]
        public Dictionary<string, InlineCompletionStates> CompletionSessionResult { get; set; }
    }
}
