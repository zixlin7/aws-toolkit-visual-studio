using System;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.Runtime;

using Xunit;

// ReSharper disable InconsistentNaming ("AWSCredentials" from AWS SDK)

namespace AWSToolkit.Tests.Credentials.Core
{
    public class ToolkitCredentialsTests
    {
        private static readonly ICredentialIdentifier SampleCredentialId = FakeCredentialIdentifier.Create("foo");
        private static readonly AWSCredentials SampleAwsCredentials = new AnonymousAWSCredentials();

        private static readonly ToolkitCredentials NullToolkitCredentials =
            new ToolkitCredentials(SampleCredentialId, null);

        private readonly ToolkitCredentials _credentialsWithAwsCredentials;

        public ToolkitCredentialsTests()
        {
            _credentialsWithAwsCredentials = new ToolkitCredentials(SampleCredentialId, SampleAwsCredentials);
        }

        [Fact]
        public void RequiresCredentialId()
        {
            Assert.Throws<ArgumentNullException>(() => new ToolkitCredentials(null, new AnonymousAWSCredentials()));
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
        public void GetAwsCredentials()
        {
            Assert.Equal(SampleAwsCredentials, _credentialsWithAwsCredentials.GetAwsCredentials());
        }

        [Fact]
        public void GetAwsCredentialsThrowsIfNull()
        {
            Assert.Throws<InvalidOperationException>(() => NullToolkitCredentials.GetAwsCredentials());
        }
    }
}
