using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.SecurityScans.Models;

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

    public enum SecurityScanStatus
    {
        Succeeded,
        Failed,
        Cancelled
    }

    public class SecurityScanResult
    {
        public SecurityScanStatus Status { get; }
        public string ScannedFiles { get; }

    }

    public class SecurityScanResponse
    {
        public SecurityScanResult SecurityScanResult { get; }
        public string Error { get; }
    }
    /// <summary>
    /// Abstraction for security scan related communications with the language server
    /// </summary>
    public interface ISecurityScans
    {
        Task<SecurityScanResponse> RunSecurityScanAsync(ExecuteCommandParams securityScanParams);
        Task CancelSecurityScanAsync();
    }

    public class SecurityScans : ISecurityScans
    {
        private readonly JsonRpc _rpc;
        public SecurityScans(JsonRpc rpc)
        {
            _rpc = rpc;
        }

        public async Task<SecurityScanResponse> RunSecurityScanAsync(ExecuteCommandParams securityScanParams)
        {
           return await _rpc.InvokeWithParameterObjectAsync<SecurityScanResponse>(
                ExecuteCommandNames.ExecuteMethod, securityScanParams);
        }

        public async Task CancelSecurityScanAsync()
        {
            var request = new ExecuteCommandParams
            {
                Command = ExecuteCommandNames.CancelSecurityScan,
            };
            await _rpc.InvokeWithParameterObjectAsync(
                 ExecuteCommandNames.ExecuteMethod, request);
        }
    }
}
