using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.SecurityScan;


namespace Amazon.AwsToolkit.CodeWhisperer.Tests.SecurityScan
{
    public class FakeSecurityScanProvider : ISecurityScanProvider
    {

        public bool DidRunSecurityScan = false;
        public virtual Task GetScanFindingsAsync()
        {
            DidRunSecurityScan = true;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
