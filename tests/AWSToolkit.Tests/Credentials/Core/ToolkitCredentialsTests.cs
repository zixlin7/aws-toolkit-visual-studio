using System;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.Runtime;

using Moq;

using Xunit;

// ReSharper disable InconsistentNaming ("AWSCredentials" from AWS SDK)

namespace AWSToolkit.Tests.Credentials.Core
{
    public class ToolkitCredentialsTests
    {
        private static readonly ICredentialIdentifier SampleCredentialId = FakeCredentialIdentifier.Create("foo");
        private static readonly AWSCredentials SampleAwsCredentials = new AnonymousAWSCredentials();

        private static readonly ToolkitCredentials NullToolkitCredentials =
            new ToolkitCredentials(SampleCredentialId, null, null);

        private readonly Mock<IAWSTokenProvider> _tokenProvider = new Mock<IAWSTokenProvider>();

        private readonly ToolkitCredentials _credentialsWithAwsCredentials;
        private readonly ToolkitCredentials _credentialsWithTokenProvider;

        public ToolkitCredentialsTests()
        {
            _credentialsWithAwsCredentials = new ToolkitCredentials(SampleCredentialId, SampleAwsCredentials);
            _credentialsWithTokenProvider = new ToolkitCredentials(SampleCredentialId, _tokenProvider.Object);
        }

        [Fact]
        public void RequiresCredentialId()
        {
            Assert.Throws<ArgumentNullException>(() => new ToolkitCredentials(null, new AnonymousAWSCredentials()));
            Assert.Throws<ArgumentNullException>(() => new ToolkitCredentials(null, _tokenProvider.Object));
        }

        [Fact]
        public void SupportsAwsCredentialsWithAWSCredentials()
        {
            Assert.True(_credentialsWithAwsCredentials.Supports(AwsConnectionType.AwsCredentials));
        }

        [Fact]
        public void DoesNotSupportAwsCredentialsWithoutAWSCredentials()
        {
            Assert.False(NullToolkitCredentials.Supports(AwsConnectionType.AwsCredentials));
        }

        [Fact]
        public void SupportsAwsTokenWithTokenProvider()
        {
            Assert.True(_credentialsWithTokenProvider.Supports(AwsConnectionType.AwsToken));
        }

        [Fact]
        public void DoesNotSupportAwsTokenWithoutTokenProvider()
        {
            Assert.False(NullToolkitCredentials.Supports(AwsConnectionType.AwsToken));
        }

        [Fact]
        public void GetAwsCredentials()
        {
            Assert.Equal(SampleAwsCredentials, _credentialsWithAwsCredentials.GetAwsCredentials());
        }

        [Fact]
        public void GetAwsCredentialsThrowsIfNull()
        {
            Assert.Throws<InvalidOperationException>(() => NullToolkitCredentials.GetAwsCredentials());
        }

        [Fact]
        public void GetTokenProvider()
        {
            Assert.Equal(_tokenProvider.Object, _credentialsWithTokenProvider.GetTokenProvider());
        }

        [Fact]
        public void GetTokenProviderThrowsIfNull()
        {
            Assert.Throws<InvalidOperationException>(() => NullToolkitCredentials.GetTokenProvider());
        }
    }
}
