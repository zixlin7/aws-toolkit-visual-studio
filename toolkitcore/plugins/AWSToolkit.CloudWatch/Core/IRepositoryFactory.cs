using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CloudWatch.Core
{
    public interface IRepositoryFactory
    {
        ICloudWatchLogsRepository CreateCloudWatchLogsRepository(AwsConnectionSettings connectionSettings);
    }
}
