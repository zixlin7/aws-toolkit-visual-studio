using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.AWSToolkit.Lsp;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials
{
    internal static class MessageNames
    {
        public const string UpdateIamCredenitals = "$/aws/credentials/iam/update";
        public const string UpdateTokenCredenitals = "$/aws/credentials/token/update";
        public const string DeleteIamCredenitals = "$/aws/credentials/iam/delete";
        public const string DeleteTokenCredenitals = "$/aws/credentials/token/delete";
    }

    /// <summary>
    /// JSON-RPC proxy for credentials related notifications and requests.
    /// Bound to a JSON-RPC session with JsonRcp.Attach
    /// see https://github.com/microsoft/vs-streamjsonrpc/blob/main/doc/sendrequest.md
    /// </summary>
    internal interface ILspCredentials
    {
        [JsonRpcMessageMapping(MessageNames.UpdateIamCredenitals)]
        Task UpdateIamCredentialsAsync(UpdateCredentialsRequest request);
        [JsonRpcMessageMapping(MessageNames.UpdateTokenCredenitals)]
        Task UpdateTokenCredentialsAsync(UpdateCredentialsRequest request);

        [JsonRpcMessageMapping(MessageNames.DeleteIamCredenitals)]
        void DeleteIamCredentials();
        [JsonRpcMessageMapping(MessageNames.DeleteTokenCredenitals)]
        void DeleteTokenCredentials();
    }
}
