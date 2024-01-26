using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;
using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.SecurityScan
{
    [Export(typeof(ISecurityScanProvider))]
    internal class SecurityScanProvider : ISecurityScanProvider
    {
        private readonly ICodeWhispererLspClient _lspClient;
        private readonly IToolkitContextProvider _toolkitContextProvider;

        [ImportingConstructor]
        public SecurityScanProvider(
            ICodeWhispererLspClient lspClient,
            IToolkitContextProvider toolkitContextProvider)
        {
            _lspClient = lspClient;
            _toolkitContextProvider = toolkitContextProvider;
        }
 
        public async Task ScanAsync()
        {
            var taskStatus = await _toolkitContextProvider.GetToolkitContext().ToolkitHost.CreateTaskStatusNotifier();
            taskStatus.Title = "Scanning active file and its dependencies...";
            taskStatus.CanCancel = true;
            var securityScan = _lspClient.CreateSecurityScan();
            var request = new SecurityScanParams();

            taskStatus.ShowTaskStatus(async _ =>
            {
                {
                    // TODO: remove the placeholder delay
                    await Task.Delay(5000);
                    await securityScan.RunSecurityScanAsync(request);
                }
            });

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
