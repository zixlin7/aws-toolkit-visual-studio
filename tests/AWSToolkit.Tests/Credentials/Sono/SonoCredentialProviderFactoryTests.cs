using System;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Sono
{
    public class SonoCredentialProviderFactoryTests
    {
        private static readonly ToolkitRegion SampleRegion = new ToolkitRegion()
        {
            DisplayName = "sample-region",
            Id = "sample-region",
        };

        private static readonly ICredentialIdentifier SonoCredentialId = new SonoCredentialIdentifier("sono");
        private static readonly ICredentialIdentifier OtherSonoCredentialId = new SonoCredentialIdentifier("other-sono");
        private static readonly ICredentialIdentifier NonSonoCredentialId = new SDKCredentialIdentifier("non-sono-sample");
        private readonly Mock<IAWSToolkitShellProvider> _toolkitShell = new Mock<IAWSToolkitShellProvider>();
        private readonly SonoCredentialProviderFactory _sut;

        public SonoCredentialProviderFactoryTests()
        {
            _sut = new SonoCredentialProviderFactory(_toolkitShell.Object);
        }

        [Fact]
        public void GetCredentialIdentifiers()
        {
            var ids = _sut.GetCredentialIdentifiers();

            var credentialId = Assert.Single<ICredentialIdentifier>(ids);
            Assert.Equal(SonoCredentialId.FactoryId, credentialId.FactoryId);
            Assert.Equal(SonoCredentialId.Id, credentialId.Id);
        }

        [Fact]
        public void CreateToolkitCredentials()
        {
            var credentials = _sut.CreateToolkitCredentials(SonoCredentialId, SampleRegion);

            Assert.True(credentials.Supports(AwsConnectionType.AwsToken));
            Assert.False(credentials.Supports(AwsConnectionType.AwsCredentials));
        }

        [Fact]
        public void CreateToolkitCredentialsShouldThrowOnDifferentFactory()
        {
            Assert.Throws<ArgumentException>(() =>
                _sut.CreateToolkitCredentials(NonSonoCredentialId, SampleRegion));
        }

        [Fact]
        public void CreateToolkitCredentialsShouldThrowOnDifferentId()
        {
            Assert.Throws<NotSupportedException>(() =>
                _sut.CreateToolkitCredentials(OtherSonoCredentialId, SampleRegion));
        }

        [Fact]
        public void IsLoginRequired()
        {
            Assert.True(_sut.IsLoginRequired(SonoCredentialId));
        }

        [Fact]
        public void Supports()
        {
            Assert.True(_sut.Supports(SonoCredentialId, AwsConnectionType.AwsToken));
            Assert.False(_sut.Supports(SonoCredentialId, AwsConnectionType.AwsCredentials));
        }

        [Fact]
        public void SupportsShouldFailWithUnexpectedId()
        {
            Assert.False(_sut.Supports(OtherSonoCredentialId, AwsConnectionType.AwsToken));
            Assert.False(_sut.Supports(OtherSonoCredentialId, AwsConnectionType.AwsCredentials));
            Assert.False(_sut.Supports(NonSonoCredentialId, AwsConnectionType.AwsToken));
            Assert.False(_sut.Supports(NonSonoCredentialId, AwsConnectionType.AwsCredentials));
        }
    }
}
