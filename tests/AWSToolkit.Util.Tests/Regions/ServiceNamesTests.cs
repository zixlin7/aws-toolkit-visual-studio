using Amazon.AWSToolkit.Regions;
using Amazon.CloudWatchLogs;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Regions
{
    public class ServiceNamesTests
    {
        [Fact]
        public void CloudWatchLogs()
        {
            Assert.Equal(new AmazonCloudWatchLogsConfig().RegionEndpointServiceName, ServiceNames.CloudWatchLogs);
        }
    }
}
