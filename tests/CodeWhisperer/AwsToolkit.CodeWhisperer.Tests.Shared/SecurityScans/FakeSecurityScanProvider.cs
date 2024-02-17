using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.SecurityScan;
using Amazon.AwsToolkit.CodeWhisperer.SecurityScans;
using Amazon.AwsToolkit.CodeWhisperer.SecurityScans.Models;


namespace Amazon.AwsToolkit.CodeWhisperer.Tests.SecurityScan
{
    public class FakeSecurityScanProvider : ISecurityScanProvider
    {
        public SecurityScanState ScanState { get; set; }

        public event EventHandler<SecurityScanStateChangedEventArgs> SecurityScanStateChanged;

        public void RaiseStateChanged()
        {
            SecurityScanStateChanged?.Invoke(this, new SecurityScanStateChangedEventArgs(ScanState));
        }

        public virtual Task ScanAsync()
        {
            ScanState = SecurityScanState.Running;
            return Task.CompletedTask;
        }

        public virtual Task CancelScanAsync()
        {
            ScanState = SecurityScanState.Cancelling;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
