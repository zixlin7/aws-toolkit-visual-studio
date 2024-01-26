using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.SecurityScans;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.SecurityScans
{
    public class FakeSecurityScans : ISecurityScans
    {
        public Task RunSecurityScanAsync(SecurityScanParams securityScanParams)
        {
            return Task.CompletedTask;
        }
    }
}
