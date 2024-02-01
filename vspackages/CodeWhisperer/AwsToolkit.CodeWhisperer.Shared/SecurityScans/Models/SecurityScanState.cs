namespace Amazon.AwsToolkit.CodeWhisperer.SecurityScans.Models
{
    public enum SecurityScanState
    {
        /// <summary>
        /// There is no security scan in progress
        /// </summary>
        NotRunning,
        /// <summary>
        /// Security scan is in progress
        /// </summary>
        Running,
        /// <summary>
        /// In progress of cancelling the security scan
        /// </summary>
        Cancelling,
    }
}
