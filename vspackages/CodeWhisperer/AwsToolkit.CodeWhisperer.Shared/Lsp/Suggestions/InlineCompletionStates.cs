using Newtonsoft.Json;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Suggestions
{
    public class InlineCompletionStates
    {
        [JsonProperty("seen")]
        public bool Seen { get; set; } = false;

        [JsonProperty("accepted")]
        public bool Accepted { get; set; } = false;

        [JsonProperty("discarded")]
        public bool Discarded { get; set; } = false;
    }
}
