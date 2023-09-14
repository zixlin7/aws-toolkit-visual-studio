using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Credentials
{
    internal class FakeLspCredentials : ILspCredentials
    {
        public string CredentialsPayload;
        public string TokenPayload;

        public void DeleteIamCredentials()
        {
            CredentialsPayload = null;
        }

        public void DeleteTokenCredentials()
        {
            TokenPayload = null;
        }

        public void UpdateIamCredentials(UpdateCredentialsRequest request)
        {
            CredentialsPayload = request.Data;
        }

        public void UpdateTokenCredentials(UpdateCredentialsRequest request)
        {
            TokenPayload = request.Data;
        }
    }
}
