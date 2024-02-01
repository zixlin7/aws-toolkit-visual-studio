using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.SecurityScans;
using Amazon.AwsToolkit.CodeWhisperer.SecurityScans.Models;

namespace Amazon.AwsToolkit.CodeWhisperer.SecurityScan
{
    /// <summary>
    /// Handles security scans
    /// </summary>
    public interface ISecurityScanProvider : IDisposable
    {
         SecurityScanState ScanState { get; }

        event EventHandler<SecurityScanStateChangedEventArgs> SecurityScanStateChanged;

        /// <summary>
        /// Start security scan
        /// </summary>
        Task ScanAsync();
        /// <summary>
        /// Cancel security scan
        /// </summary>
        Task CancelScanAsync();
    }
}
