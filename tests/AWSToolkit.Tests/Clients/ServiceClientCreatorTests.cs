using System;

using Amazon;
using Amazon.AWSToolkit.Clients;
using Amazon.EC2;
using Amazon.Runtime;
using Xunit;

namespace AWSToolkit.Tests.Clients
{
    public abstract class FakeAbstractServiceClient : AmazonServiceClient
    {
        protected FakeAbstractServiceClient(AWSCredentials credentials, ClientConfig config) : base(credentials, config)
        {
        }

        protected FakeAbstractServiceClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, ClientConfig config) : base(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, config)
        {
        }

        protected FakeAbstractServiceClient(string awsAccessKeyId, string awsSecretAccessKey, ClientConfig config) : base(awsAccessKeyId, awsSecretAccessKey, config)
        {
        }
    }

    public class ServiceClientCreatorTests
    {
        private readonly AWSCredentials _credentials = new AnonymousAWSCredentials();
        private readonly RegionEndpoint _regionEndpoint = RegionEndpoint.CACentral1;

        public static TheoryData<Type> GetInvalidClientTypes()
        {
            return new TheoryData<Type>()
            {
                // Not a service client
                typeof(AmazonEC2Config),

                // Interface (instead of class)
                typeof(IAmazonEC2),

                // Abstract class (cannot be instantiated)
                typeof(FakeAbstractServiceClient)
            };
        }

        [Fact]
        public void CreateServiceClient_RegionEndpoint()
        {
            var client = ServiceClientCreator.CreateServiceClient(typeof(AmazonEC2Client), _credentials, _regionEndpoint);
            Assert.NotNull(client);
            var ec2Client = Assert.IsType<AmazonEC2Client>(client);
            Assert.Equal(_regionEndpoint.SystemName, ec2Client.Config.RegionEndpoint.SystemName);
        }

        [Theory]
        [MemberData(nameof(GetInvalidClientTypes))]
        public void CreateServiceClient_RegionEndpoint_InvalidTypes(Type serviceClientType)
        {
            var client = ServiceClientCreator.CreateServiceClient(serviceClientType, _credentials, _regionEndpoint);
            Assert.Null(client);
        }

        [Fact]
        public void CreateServiceClient_CredentialsAndClientConfig()
        {
            var clientConfig = new AmazonEC2Config();
            clientConfig.ServiceURL = "https://abcxyz.com";
            var client = ServiceClientCreator.CreateServiceClient(typeof(AmazonEC2Client), _credentials, clientConfig);
            Assert.NotNull(client);
            var ec2Client = Assert.IsType<AmazonEC2Client>(client);
            Assert.Equal(clientConfig.ServiceURL, ec2Client.Config.ServiceURL);
        }

        [Theory]
        [MemberData(nameof(GetInvalidClientTypes))]
        public void CreateServiceClient_CredentialsAndClientConfig_InvalidTypes(Type serviceClientType)
        {
            var clientConfig = new AmazonEC2Config();
            clientConfig.ServiceURL = "https://abcxyz.com";
            var client = ServiceClientCreator.CreateServiceClient(serviceClientType, _credentials, clientConfig);
            Assert.Null(client);
        }

        [Fact]
        public void CreateServiceClient_ClientConfig()
        {
            var clientConfig = new AmazonEC2Config();
            clientConfig.ServiceURL = "https://abcxyz.com";
            var client = ServiceClientCreator.CreateServiceClient(typeof(AmazonEC2Client), clientConfig);
            Assert.NotNull(client);
            var ec2Client = Assert.IsType<AmazonEC2Client>(client);
            Assert.Equal(clientConfig.ServiceURL, ec2Client.Config.ServiceURL);
        }

        [Theory]
        [MemberData(nameof(GetInvalidClientTypes))]
        public void CreateServiceClient_ClientConfig_InvalidTypes(Type serviceClientType)
        {
            var clientConfig = new AmazonEC2Config();
            clientConfig.ServiceURL = "https://abcxyz.com";
            var client = ServiceClientCreator.CreateServiceClient(serviceClientType, clientConfig);
            Assert.Null(client);
        }
    }
}
