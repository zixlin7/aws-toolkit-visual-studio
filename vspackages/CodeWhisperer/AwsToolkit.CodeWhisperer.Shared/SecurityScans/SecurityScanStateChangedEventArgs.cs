using System;

using Amazon.AwsToolkit.CodeWhisperer.SecurityScans.Models;

namespace Amazon.AwsToolkit.CodeWhisperer.SecurityScans
{
    public class SecurityScanStateChangedEventArgs : EventArgs
    {
        public SecurityScanStateChangedEventArgs(SecurityScanState scanState)
        {
            ScanState = scanState;
        }

        public SecurityScanState ScanState { get; }
    }
}
