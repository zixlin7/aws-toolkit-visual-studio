using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.Runtime;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials
{
    public interface IToolkitLspCredentials
    {
        void DeleteCredentials();
        void DeleteToken();

        void UpdateCredentials(ImmutableCredentials credentials);
        void UpdateToken(BearerToken token);
    }
}
