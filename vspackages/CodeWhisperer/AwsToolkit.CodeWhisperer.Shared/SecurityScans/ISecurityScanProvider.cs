using System;
using System.Threading.Tasks;

namespace Amazon.AwsToolkit.CodeWhisperer.SecurityScan
{
    /// <summary>
    /// Handles security scans
    /// </summary>
    public interface ISecurityScanProvider : IDisposable
    {
        /// <summary>
        /// Start security scan
        /// </summary>
        Task GetScanFindingsAsync();
    }
}
