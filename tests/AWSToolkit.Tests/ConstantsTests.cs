using Amazon;
using Amazon.AWSToolkit;
using Amazon.EC2;
using Amazon.ElasticBeanstalk;
using Xunit;

namespace AWSToolkit.Tests
{
    public class ConstantsTests
    {
        [Fact]
        public void GetServicePrincipalForAssumeRole()
        {
            var serviceName = new AmazonEC2Config().RegionEndpointServiceName;

            Assert.Equal("ec2.amazonaws.com",
                Constants.GetServicePrincipalForAssumeRole(RegionEndpoint.USWest2.SystemName, serviceName));
            Assert.Equal("ec2.amazonaws.com.cn",
                Constants.GetServicePrincipalForAssumeRole(RegionEndpoint.CNNorthWest1.SystemName, serviceName));
        }

        [Fact]
        public void GetServicePrincipalForAssumeRole_ElasticBeanstalk()
        {
            var expectedValue = "elasticbeanstalk.amazonaws.com";
            var serviceName = new AmazonElasticBeanstalkConfig().RegionEndpointServiceName;

            Assert.Equal(expectedValue,
                Constants.GetServicePrincipalForAssumeRole(RegionEndpoint.USWest2.SystemName, serviceName));
            Assert.Equal(expectedValue,
                Constants.GetServicePrincipalForAssumeRole(RegionEndpoint.CNNorthWest1.SystemName, serviceName));
        }
    }
}
