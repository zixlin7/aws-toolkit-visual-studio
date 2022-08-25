using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CloudWatch.Logs.Core
{
    public interface IRepositoryFactory
    {
        ICloudWatchLogsRepository CreateCloudWatchLogsRepository(AwsConnectionSettings connectionSettings);
    }
}
