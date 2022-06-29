using Amazon.AWSToolkit.Regions;
using Amazon.CloudWatchLogs;
using Amazon.ECS;
using Amazon.Lambda;

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

        [Fact]
        public void Lambda()
        {
            Assert.Equal(new AmazonLambdaConfig().RegionEndpointServiceName, ServiceNames.Lambda);
        }

        [Fact]
        public void Ecs()
        {
            Assert.Equal(new AmazonECSConfig().RegionEndpointServiceName, ServiceNames.Ecs);
        }
    }
}
