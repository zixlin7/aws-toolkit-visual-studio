using Amazon;
using Amazon.AWSToolkit.Clients;
using Amazon.EC2;
using Amazon.Runtime;
using Xunit;

namespace AWSToolkit.Tests.Clients
{
    public class ServiceClientCreatorTests
    {
        private readonly AWSCredentials _credentials = new AnonymousAWSCredentials();
        private readonly RegionEndpoint _regionEndpoint = RegionEndpoint.CACentral1;

        [Fact]
        public void CreateServiceClient_RegionEndpoint()
        {
            var client = ServiceClientCreator.CreateServiceClient(typeof(AmazonEC2Client), _credentials, _regionEndpoint);
            Assert.NotNull(client);
            var ec2Client = Assert.IsType<AmazonEC2Client>(client);
            Assert.Equal(_regionEndpoint.SystemName, ec2Client.Config.RegionEndpoint.SystemName);
        }

        [Fact]
        public void CreateServiceClient_RegionEndpoint_NonServiceType()
        {
            var client = ServiceClientCreator.CreateServiceClient(typeof(AmazonEC2Config), _credentials, _regionEndpoint);
            Assert.Null(client);
        }

        [Fact]
        public void CreateServiceClient_CredentialsAndClientConfig()
        {
            var clientConfig = new AmazonEC2Config();
            clientConfig.ServiceURL = "some-url";
            var client = ServiceClientCreator.CreateServiceClient(typeof(AmazonEC2Client), _credentials, clientConfig);
            Assert.NotNull(client);
            var ec2Client = Assert.IsType<AmazonEC2Client>(client);
            Assert.Equal(clientConfig.ServiceURL, ec2Client.Config.ServiceURL);
        }

        [Fact]
        public void CreateServiceClient_ClientConfig()
        {
            var clientConfig = new AmazonEC2Config();
            clientConfig.ServiceURL = "some-url";
            var client = ServiceClientCreator.CreateServiceClient(typeof(AmazonEC2Client), clientConfig);
            Assert.NotNull(client);
            var ec2Client = Assert.IsType<AmazonEC2Client>(client);
            Assert.Equal(clientConfig.ServiceURL, ec2Client.Config.ServiceURL);
        }
    }
}
