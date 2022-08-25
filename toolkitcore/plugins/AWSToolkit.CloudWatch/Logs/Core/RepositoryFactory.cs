using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.CloudWatchLogs;

namespace Amazon.AWSToolkit.CloudWatch.Logs.Core
{
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly ToolkitContext _toolkitContext;

        public RepositoryFactory(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public ICloudWatchLogsRepository CreateCloudWatchLogsRepository(AwsConnectionSettings connectionSettings)
        {
            var cwlClient =
                _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonCloudWatchLogsClient>(connectionSettings?.CredentialIdentifier, connectionSettings?.Region);
            return new CloudWatchLogsRepository(connectionSettings, cwlClient);
        }
    }
}
