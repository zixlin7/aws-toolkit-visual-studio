using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;
using Amazon.AwsToolkit.CodeWhisperer.SecurityScans;
using Amazon.AwsToolkit.CodeWhisperer.SecurityScans.Models;
using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.SecurityScan
{
    [Export(typeof(ISecurityScanProvider))]
    internal class SecurityScanProvider : ISecurityScanProvider
    {
        private readonly ICodeWhispererLspClient _lspClient;
        private readonly IToolkitContextProvider _toolkitContextProvider;
        private SecurityScanState _scanState = SecurityScanState.NotRunning;
        public SecurityScanState ScanState
        {
            get => _scanState;
            set
            {
                if (_scanState != value)
                {
                    _scanState = value;
                    RaiseStatusChanged(value);
                }
            }
        }

        [ImportingConstructor]
        public SecurityScanProvider(
            ICodeWhispererLspClient lspClient,
            IToolkitContextProvider toolkitContextProvider)
        {
            _lspClient = lspClient;
            _toolkitContextProvider = toolkitContextProvider;
        }

        public event EventHandler<SecurityScanStateChangedEventArgs> SecurityScanStateChanged;

        public async Task ScanAsync()
        {
            ScanState = SecurityScanState.Running;
            var taskStatus = await _toolkitContextProvider.GetToolkitContext().ToolkitHost.CreateTaskStatusNotifier();
            taskStatus.Title = "Scanning active file and its dependencies...";
            taskStatus.CanCancel = false;
            var securityScan = _lspClient.CreateSecurityScan();
            var request = new SecurityScanParams();

            taskStatus.ShowTaskStatus(async _ =>
            {
                {
                    // TODO: remove the placeholder delay
                    await Task.Delay(5000);
                    await securityScan.RunSecurityScanAsync(request);
                    ScanState = SecurityScanState.NotRunning;
                }
            });

        }

        public async Task CancelScanAsync()
        {
            ScanState = SecurityScanState.Cancelling;
            var securityScan = _lspClient.CreateSecurityScan();
            await securityScan.CancelSecurityScanAsync();
            ScanState = SecurityScanState.NotRunning;
        }

        private void RaiseStatusChanged(SecurityScanState securityScanState)
        {
            SecurityScanStateChanged?.Invoke(this, new SecurityScanStateChangedEventArgs(securityScanState));
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
