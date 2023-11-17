using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.Runtime;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Credentials
{
    public class FakeToolkitLspCredentials : IToolkitLspCredentials
    {
        public ImmutableCredentials CredentialsPayload;
        public BearerToken TokenPayload;

        public void DeleteCredentials()
        {
            CredentialsPayload = null;
        }

        public void DeleteToken()
        {
            TokenPayload = null;
        }

        public Task UpdateCredentialsAsync(ImmutableCredentials credentials)
        {
            CredentialsPayload = credentials;
            return Task.CompletedTask;
        }

        public Task UpdateTokenAsync(BearerToken token)
        {
            TokenPayload = token;
            return Task.CompletedTask;
        }
    }
}
