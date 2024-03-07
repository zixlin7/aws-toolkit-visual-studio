using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServer.Protocol;

using StreamJsonRpc;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.SecurityScans
{
    internal static class ExecuteCommandNames
    {
        public const string ExecuteMethod = "workspace/executeCommand";
        public const string RunSecurityScan = "aws/codewhisperer/runSecurityScan";
        public const string CancelSecurityScan = "aws/codewhisperer/cancelSecurityScan";
    }

    /// <summary>
    /// Abstraction for security scan related communications with the language server
    /// </summary>
    public interface ISecurityScans
    {
        Task RunSecurityScanAsync(ExecuteCommandParams securityScanParams);
        Task CancelSecurityScanAsync();
    }

    public class SecurityScans : ISecurityScans
    {
        private readonly JsonRpc _rpc;
        public SecurityScans(JsonRpc rpc)
        {
            _rpc = rpc;
        }

        public async Task RunSecurityScanAsync(ExecuteCommandParams securityScanParams)
        {
           await _rpc.InvokeWithParameterObjectAsync(
                ExecuteCommandNames.ExecuteMethod, securityScanParams);
        }

        public async Task CancelSecurityScanAsync()
        {
            var request = new ExecuteCommandParams
            {
                Command = ExecuteCommandNames.CancelSecurityScan,
            };
            await _rpc.InvokeAsync(
                 ExecuteCommandNames.ExecuteMethod, request);
        }
    }
}
