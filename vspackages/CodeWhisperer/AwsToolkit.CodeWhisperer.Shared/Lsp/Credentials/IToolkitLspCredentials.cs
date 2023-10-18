using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.Runtime;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials
{
    public interface IToolkitLspCredentials
    {
        void DeleteCredentials();
        void DeleteToken();

        Task UpdateCredentialsAsync(ImmutableCredentials credentials);
        Task UpdateTokenAsync(BearerToken token);
    }
}
