using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;

using StreamJsonRpc;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.SecurityScans
{
    internal static class MessageNames
    {
        // TODO: replace with finalized message name
        public const string RunSecurityScan = "aws/codewhisperer/securityscan";
        public const string CancelSecurityScan = "aws/codewhisperer/cancelsecurityscan";
    }

    /// <summary>
    /// Abstraction for security scan related communications with the language server
    /// </summary>
    public interface ISecurityScans
    {
        Task RunSecurityScanAsync(SecurityScanParams securityScanParams);
        Task CancelSecurityScanAsync();
    }

    public class SecurityScans : ISecurityScans
    {
        private readonly JsonRpc _rpc;
        public SecurityScans(JsonRpc rpc)
        {
            _rpc = rpc;
        }

        public async Task RunSecurityScanAsync(SecurityScanParams securityScanParams)
        {
           await _rpc.InvokeWithParameterObjectAsync(
                MessageNames.RunSecurityScan, securityScanParams);
        }

        public async Task CancelSecurityScanAsync()
        {
            await _rpc.InvokeAsync(
                 MessageNames.CancelSecurityScan);
        }
    }
}
