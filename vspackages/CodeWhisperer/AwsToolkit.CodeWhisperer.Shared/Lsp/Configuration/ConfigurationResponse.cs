using Newtonsoft.Json;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Configuration
{
    /// <summary>
    /// The language server's Configuration structure.
    /// Used with "configuration request" and "configuration changed" messages.
    /// </summary>
    public class ConfigurationResponse
    {
        [JsonProperty("aws")]
        public AwsConfigurationResponse Aws { get; set; } = new AwsConfigurationResponse();

    }

    public class AwsConfigurationResponse
    {
        [JsonProperty("codeWhisperer")]
        public CodeWhispererConfigurationResponse CodeWhisperer { get; set; } = null;
    }

    public class CodeWhispererConfigurationResponse
    {
        [JsonProperty("includeSuggestionsWithCodeReferences")]
        public bool IncludeSuggestionsWithCodeReferences { get; set; }
    }
}
