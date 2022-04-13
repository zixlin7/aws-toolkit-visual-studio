using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CloudWatch.Core
{
    public interface IRepositoryFactory
    {
        ICloudWatchLogsRepository CreateCloudWatchLogsRepository(ICredentialIdentifier credentialsId,
            ToolkitRegion region);
    }
}
