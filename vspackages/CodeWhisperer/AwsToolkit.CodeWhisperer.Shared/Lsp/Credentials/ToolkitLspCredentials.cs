using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.Runtime;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials
{
    internal class ToolkitLspCredentials : IToolkitLspCredentials
    {
        private readonly CredentialsEncryption _encryptor;
        private readonly ILspCredentials _credentialsProtocol;

        public ToolkitLspCredentials(CredentialsEncryption encryptor, ILspCredentials credentialsProtocol)
        {
            _encryptor = encryptor;
            _credentialsProtocol = credentialsProtocol;
        }

        /// <summary>
        /// Encrypt and transmit SigV4 credentials to language server
        /// </summary>
        public async Task UpdateCredentialsAsync(ImmutableCredentials credentials)
        {
            var request = _encryptor.CreateUpdateCredentialsRequest(credentials);
            await _credentialsProtocol.UpdateIamCredentialsAsync(request);
        }

        /// <summary>
        /// Encrypt and transmit bearer token to language server
        /// </summary>
        public async Task UpdateTokenAsync(BearerToken token)
        {
            var request = _encryptor.CreateUpdateCredentialsRequest(token);
            await _credentialsProtocol.UpdateTokenCredentialsAsync(request);
        }

        /// <summary>
        /// Remove SigV4 based credentials from language server
        /// </summary>
        public void DeleteCredentials()
        {
            _credentialsProtocol.DeleteIamCredentials();
        }

        /// <summary>
        /// Remove bearer token from language server
        /// </summary>
        public void DeleteToken()
        {
            _credentialsProtocol.DeleteTokenCredentials();
        }
    }
}
