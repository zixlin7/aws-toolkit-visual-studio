using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Clients
{
    public class FakeToolkitLspClient : IToolkitLspClient
    {
        public IToolkitLspCredentials CreateToolkitLspCredentials()
        {
            // todo : implement a fake IToolkitLspCredentials when it is needed in testing
            throw new System.NotImplementedException();
        }
    }
}
