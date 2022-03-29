using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.CloudWatchLogs;

namespace Amazon.AWSToolkit.CloudWatch.Core
{
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly ToolkitContext _toolkitContext;

        public RepositoryFactory(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public ICloudWatchLogsRepository CreateCloudWatchLogsRepository(ICredentialIdentifier credentialsId,
            ToolkitRegion region)
        {
            var cwlClient =
                _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonCloudWatchLogsClient>(credentialsId,
                    region);
            return new CloudWatchLogsRepository(credentialsId, region, cwlClient);
        }
    }
}
