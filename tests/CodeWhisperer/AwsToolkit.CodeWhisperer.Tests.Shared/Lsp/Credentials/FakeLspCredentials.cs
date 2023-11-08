using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Credentials
{
    internal class FakeLspCredentials : ILspCredentials
    {
        public string CredentialsPayload;
        public string TokenPayload;

        public virtual void DeleteIamCredentials()
        {
            CredentialsPayload = null;
        }

        public virtual void DeleteTokenCredentials()
        {
            TokenPayload = null;
        }

        public virtual Task UpdateIamCredentialsAsync(UpdateCredentialsRequest request)
        {
            CredentialsPayload = request.Data;
            return Task.CompletedTask;
        }

        public virtual Task UpdateTokenCredentialsAsync(UpdateCredentialsRequest request)
        {
            TokenPayload = request.Data;
            return Task.CompletedTask;
        }
    }
}
