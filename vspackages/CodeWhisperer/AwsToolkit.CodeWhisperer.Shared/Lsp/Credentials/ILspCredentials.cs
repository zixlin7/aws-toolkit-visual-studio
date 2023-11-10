using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.AWSToolkit.Lsp;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials
{
    internal static class MessageNames
    {
        public const string UpdateIamCredentials = "aws/credentials/iam/update";
        public const string UpdateTokenCredentials = "aws/credentials/token/update";
        public const string DeleteIamCredentials = "aws/credentials/iam/delete";
        public const string DeleteTokenCredentials = "aws/credentials/token/delete";
    }

    /// <summary>
    /// JSON-RPC proxy for credentials related notifications and requests.
    /// Bound to a JSON-RPC session with JsonRcp.Attach
    /// see https://github.com/microsoft/vs-streamjsonrpc/blob/main/doc/sendrequest.md
    /// </summary>
    internal interface ILspCredentials
    {
        [JsonRpcMessageMapping(MessageNames.UpdateIamCredentials)]
        Task UpdateIamCredentialsAsync(UpdateCredentialsRequest request);
        [JsonRpcMessageMapping(MessageNames.UpdateTokenCredentials)]
        Task UpdateTokenCredentialsAsync(UpdateCredentialsRequest request);

        [JsonRpcMessageMapping(MessageNames.DeleteIamCredentials)]
        void DeleteIamCredentials();
        [JsonRpcMessageMapping(MessageNames.DeleteTokenCredentials)]
        void DeleteTokenCredentials();
    }
}
