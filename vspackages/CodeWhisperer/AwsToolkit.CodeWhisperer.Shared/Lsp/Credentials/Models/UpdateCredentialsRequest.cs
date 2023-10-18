using Newtonsoft.Json;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models
{
    public class UpdateCredentialsRequest
    {
        /// <summary>
        /// Encrypted token (JWT or PASETO)
        /// The token's contents differ whether IAM or Bearer token is sent
        /// </summary>
        [JsonProperty("data")]
        public string Data;

        /// <summary>
        /// Signals that <see cref="Data"/> contains encrypted contents.
        /// </summary>
        /// <remarks>
        /// The Toolkit will always transmit encrypted data.
        /// This field is for browser based clients.
        /// </remarks>
        [JsonProperty("encrypted")]
        public bool Encrypted = true;
    }
}
