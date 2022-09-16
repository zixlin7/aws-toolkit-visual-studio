using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Clients
{
    public class AwsServiceClientManagerTests
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly Mock<IAWSTokenProvider> _tokenProvider = new Mock<IAWSTokenProvider>();
        private readonly AwsServiceClientManager _sut;
        private readonly ICredentialIdentifier _credentialId = new SharedCredentialIdentifier("profile-name");
        private readonly ToolkitRegion _sampleRegion = new ToolkitRegion()
        {
            PartitionId = "aws",
            Id = "us-west-2",
            DisplayName = "us west two",
        };
        private readonly string _localServiceUrl = "http://aws.amazon.com/";
        private bool _isRegionLocal;

        public AwsServiceClientManagerTests()
        {
            _toolkitContextFixture.RegionProvider.Setup(mock => mock.GetRegion(It.IsAny<string>()))
                .Returns<string>(regionId => regionId == _sampleRegion.Id ? _sampleRegion : null);

            _toolkitContextFixture.RegionProvider.Setup(mock =>
                mock.IsRegionLocal(It.IsAny<string>())).Returns(() => _isRegionLocal);
            _toolkitContextFixture.RegionProvider.Setup(mock =>
                mock.GetLocalEndpoint(It.IsAny<string>())).Returns(_localServiceUrl);

            _toolkitContextFixture.CredentialManager.Setup(mock =>
                    mock.GetAwsCredentials(It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()))
                .Returns<ICredentialIdentifier, ToolkitRegion>((credentialId, region) =>
                    credentialId.Id == _credentialId.Id ? new AnonymousAWSCredentials() : null);
            _toolkitContextFixture.CredentialManager.Setup(mock =>
                    mock.GetToolkitCredentials(It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()))
                .Returns<ICredentialIdentifier, ToolkitRegion>((credentialId, region) =>
                    credentialId.Id == _credentialId.Id ? new ToolkitCredentials(credentialId, new AnonymousAWSCredentials()) : null);

            _sut = new AwsServiceClientManager(_toolkitContextFixture.CredentialManager.Object,
                _toolkitContextFixture.RegionProvider.Object);
        }

        [Fact]
        public void CreateServiceClient_WithRegion()
        {
            var client = _sut.CreateServiceClient<AmazonEC2Client>(_credentialId, _sampleRegion);
            Assert.NotNull(client);
            var ec2Client = Assert.IsType<AmazonEC2Client>(client);
            Assert.Equal(_sampleRegion.Id, ec2Client.Config.RegionEndpoint.SystemName);
        }

        [Fact]
        public void CreateServiceClient_WithRegionId()
        {
            var client = _sut.CreateServiceClient<AmazonEC2Client>(_credentialId, _sampleRegion.Id);
            Assert.NotNull(client);
            var ec2Client = Assert.IsType<AmazonEC2Client>(client);
            Assert.Equal(_sampleRegion.Id, ec2Client.Config.RegionEndpoint.SystemName);
        }

        [Fact]
        public void CreateServiceClient_TokenProvider_WithRegion()
        {
            SetupCredentialManagerToReturnTokenProvider();
            var client = _sut.CreateServiceClient<AmazonEC2Client>(_credentialId, _sampleRegion);
            // This overload does not support token providers
            Assert.Null(client);
        }

        [Fact]
        public void CreateServiceClient_UnknownRegionId()
        {
            var toolkitRegion = new ToolkitRegion()
            {
                Id = "some-region-id"
            };

            Assert.Null(_sut.CreateServiceClient<AmazonEC2Client>(_credentialId, toolkitRegion.Id));
        }

        [Fact]
        public void CreateServiceClient_LocalRegion()
        {
            _isRegionLocal = true;

            var client = _sut.CreateServiceClient<AmazonEC2Client>(_credentialId, _sampleRegion);
            Assert.NotNull(client);
            Assert.IsType<AmazonEC2Client>(client);
            Assert.Equal(_localServiceUrl, client.Config.ServiceURL);
        }

        [Fact]
        public void CreateServiceClient_LocalRegionWithoutService()
        {
            _isRegionLocal = true;
            _toolkitContextFixture.RegionProvider.Setup(mock =>
                mock.GetLocalEndpoint(It.IsAny<string>())).Returns<string>(null);

            var client = _sut.CreateServiceClient<AmazonEC2Client>(_credentialId, _sampleRegion);
            Assert.Null(client);
        }

        [Fact]
        public void CreateServiceClient_UnknownCredentialId()
        {
            Assert.Null(_sut.CreateServiceClient<AmazonEC2Client>(
                new SharedCredentialIdentifier("some-credential-id"),
                _sampleRegion));
        }

        [Fact]
        public void CreateServiceClient_NonServiceClient()
        {
            Assert.Null(_sut.CreateServiceClient<AmazonEC2Config>(
                _credentialId,
                _sampleRegion));
        }

        [Fact]
        public void CreateServiceClient_LocalRegionAndConfig()
        {
            _isRegionLocal = true;

            var config = new AmazonEC2Config()
            {
                BufferSize = 123
            };

            var client = _sut.CreateServiceClient<AmazonEC2Client>(_credentialId, _sampleRegion, config);
            Assert.NotNull(client);
            Assert.IsType<AmazonEC2Client>(client);
            Assert.Equal(_localServiceUrl, client.Config.ServiceURL);
            Assert.Null(client.Config.RegionEndpoint);
            Assert.Equal(config.BufferSize, client.Config.BufferSize);
        }

        [Fact]
        public void CreateServiceClient_RegionAndConfig()
        {
            var config = new AmazonEC2Config()
            {
                BufferSize = 123
            };

            var client = _sut.CreateServiceClient<AmazonEC2Client>(_credentialId, _sampleRegion, config);
            Assert.NotNull(client);
            Assert.IsType<AmazonEC2Client>(client);
            Assert.Equal(_sampleRegion.Id, client.Config.RegionEndpoint.SystemName);
            Assert.Null(client.Config.ServiceURL);
            Assert.Equal(config.BufferSize, client.Config.BufferSize);
        }

        [Fact]
        public void CreateServiceClient_TokenProvider_RegionAndConfig()
        {
            SetupCredentialManagerToReturnTokenProvider();

            var config = new AmazonEC2Config()
            {
                ServiceURL = "http://sample-url/",
            };

            var client = _sut.CreateServiceClient<AmazonEC2Client>(_credentialId, _sampleRegion, config);
            Assert.IsType<AmazonEC2Client>(client);
            Assert.Equal(_tokenProvider.Object, client.Config.AWSTokenProvider);
            Assert.Equal("http://sample-url/", client.Config.ServiceURL);
        }

        /*
         * Summary:
         * This test confirms that using a localhost client no longer returns the AmazonServiceException:
         * "Unable to get IAM security credentials from EC2 Instance Metadata Service".
         * A successful test run catches a different AmazonEC2Exception due to the given _localServiceUrl that
         * occurs further downstream of the IAM credentials error (on the sdk side after invoking the client)
         */
        [Fact]
        public void CreateServiceClient_LocalClientShouldUseMockCredentials()
        {
            _isRegionLocal = true;
            var config = new AmazonEC2Config() { BufferSize = 123 };
            var client = _sut.CreateServiceClient<AmazonEC2Client>(_credentialId, _sampleRegion, config);
            CreateKeyPairRequest request = new CreateKeyPairRequest("testKey");
            Assert.Throws<AmazonEC2Exception>(() => client.CreateKeyPair(request));
        }

        private void SetupCredentialManagerToReturnTokenProvider()
        {
            _toolkitContextFixture.CredentialManager.Setup(mock =>
                    mock.GetToolkitCredentials(It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()))
                .Returns<ICredentialIdentifier, ToolkitRegion>((credentialId, region) =>
                    new ToolkitCredentials(credentialId, _tokenProvider.Object));
        }
    }
}
