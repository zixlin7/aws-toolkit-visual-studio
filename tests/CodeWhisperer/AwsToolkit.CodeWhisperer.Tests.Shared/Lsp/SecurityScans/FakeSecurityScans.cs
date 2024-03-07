using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.SecurityScans;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.SecurityScans
{
    public class FakeSecurityScans : ISecurityScans
    {
        public Task RunSecurityScanAsync(ExecuteCommandParams securityScanParams)
        {
            return Task.CompletedTask;
        }

        public Task CancelSecurityScanAsync()
        {
            return Task.CompletedTask;
        }
    }
}
