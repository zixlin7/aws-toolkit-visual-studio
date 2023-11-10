using Newtonsoft.Json;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models
{
    internal static class ConnectionMessageNames
    {
        /// <summary>
        /// Connection metadata request sent by the language server
        /// </summary>
        public const string ConnectionMetadataRequested = "aws/credentials/getConnectionMetadata";
    }

    /// <summary>
    /// Auth connection metadata to be sent to the server
    /// </summary>
    public class ConnectionMetadata
    {
        [JsonProperty("sso")]
        public SsoProfileData Sso { get; set; }
    }

    /// <summary>
    /// Information about sso profile
    /// </summary>
    public class SsoProfileData
    {
        [JsonProperty("startUrl")]
        public string StartUrl { get; set; }
    }
}
