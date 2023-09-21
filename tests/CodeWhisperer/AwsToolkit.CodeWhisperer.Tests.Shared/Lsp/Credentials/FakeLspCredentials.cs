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

        public virtual void UpdateIamCredentials(UpdateCredentialsRequest request)
        {
            CredentialsPayload = request.Data;
        }

        public virtual void UpdateTokenCredentials(UpdateCredentialsRequest request)
        {
            TokenPayload = request.Data;
        }
    }
}
