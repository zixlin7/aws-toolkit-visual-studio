using Newtonsoft.Json;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models
{
    public abstract class CredentialsData
    {
    }

    /// <summary>
    /// The data payload for <see cref="UpdateCredentialsRequest"/> when transmitting credentials used in SigV4 based
    /// AWS service clients.
    /// </summary>
    public class Sigv4Credentials : CredentialsData
    {
        [JsonProperty("accessKeyId")]
        public string AccessKeyId;

        [JsonProperty("secretAccessKey")]
        public string SecretAccessKey;

        [JsonProperty("sessionToken")]
        public string SessionToken;
    }

    /// <summary>
    /// The data payload for <see cref="UpdateCredentialsRequest"/> when transmitting bearer tokens used in token provider based
    /// AWS service clients.
    /// </summary>
    public class BearerToken : CredentialsData
    {
        [JsonProperty("token")]
        public string Token;
    }
}
