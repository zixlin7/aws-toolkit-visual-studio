using Newtonsoft.Json;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models
{
    public class UpdateCredentialsRequest
    {
        /**
         * Encrypted token (JWT or PASETO)
         * The token's contents differ whether IAM or Bearer token is sent
         */
        [JsonProperty("data")]
        public string Data;
    }
}
