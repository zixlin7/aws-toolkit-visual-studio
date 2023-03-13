using Amazon.AWSToolkit.Regions;
using Amazon.CloudWatchLogs;
using Amazon.CodeCatalyst;
using Amazon.CodeCommit;
using Amazon.ECS;
using Amazon.ElasticBeanstalk;
using Amazon.Lambda;
using Amazon.XRay;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Regions
{
    public class ServiceNamesTests
    {
        [Fact]
        public void Beanstalk()
        {
            Assert.Equal(new AmazonElasticBeanstalkConfig().RegionEndpointServiceName, ServiceNames.Beanstalk);
        }

        public void CloudWatchLogs()
        {
            Assert.Equal(new AmazonCloudWatchLogsConfig().RegionEndpointServiceName, ServiceNames.CloudWatchLogs);
        }

        [Fact]
        public void CodeCatalyst()
        {
            Assert.Equal(new AmazonCodeCatalystConfig().RegionEndpointServiceName, ServiceNames.CodeCatalyst);
        }

        [Fact]
        public void CodeCommit()
        {
            Assert.Equal(new AmazonCodeCommitConfig().RegionEndpointServiceName, ServiceNames.CodeCommit);
        }

        [Fact]
        public void Ecs()
        {
            Assert.Equal(new AmazonECSConfig().RegionEndpointServiceName, ServiceNames.Ecs);
        }

        [Fact]
        public void Lambda()
        {
            Assert.Equal(new AmazonLambdaConfig().RegionEndpointServiceName, ServiceNames.Lambda);
        }

        [Fact]
        public void Xray()
        {
            Assert.Equal(new AmazonXRayConfig().RegionEndpointServiceName, ServiceNames.Xray);
        }
    }
}
