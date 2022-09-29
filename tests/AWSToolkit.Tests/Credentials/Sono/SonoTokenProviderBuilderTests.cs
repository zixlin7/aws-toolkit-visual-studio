using System;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Shared;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Sono
{
    public class SonoTokenProviderBuilderTests
    {
        public static readonly TheoryData<string> NullOrWhitespaceText = new TheoryData<string>() { string.Empty, " ", null };

        private static readonly ICredentialIdentifier SonoCredentialId = new SonoCredentialIdentifier("default");
        
        private readonly Mock<IAWSToolkitShellProvider> _toolkitShell = new Mock<IAWSToolkitShellProvider>();

        [Fact]
        public void Build()
        {
            var tokenProvider = SonoTokenProviderBuilder.Create()
                .WithCredentialIdentifier(SonoCredentialId)
                .WithToolkitShell(_toolkitShell.Object)
                .Build();

            Assert.NotNull(tokenProvider);
        }

        [Fact]
        public void BuildShouldThrowWithMissingToolkitShell()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => PopulateBuilder()
                .WithToolkitShell(null)
                .Build());

            Assert.Contains("Toolkit shell", exception.Message);
        }

        [Fact]
        public void BuildShouldThrowWithMissingCredentialId()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => PopulateBuilder()
                .WithCredentialIdentifier(null)
                .Build());

            Assert.Contains("Credential", exception.Message);
        }

        [Theory]
        [MemberData(nameof(NullOrWhitespaceText))]
        public void BuildShouldThrowWithMissingSessionName(string sessionName)
        {
            var exception = Assert.Throws<InvalidOperationException>(() => PopulateBuilder()
                .WithSessionName(sessionName)
                .Build());

            Assert.Contains("Session", exception.Message);
        }

        [Fact]
        public void BuildShouldThrowWithMissingProviderRegion()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => PopulateBuilder()
                .WithTokenProviderRegion(null)
                .Build());

            Assert.Contains("Token Provider", exception.Message);
        }

        [Theory]
        [MemberData(nameof(NullOrWhitespaceText))]
        public void BuildShouldThrowWithMissingStartUrl(string startUrl)
        {
            var exception = Assert.Throws<InvalidOperationException>(() => PopulateBuilder()
                .WithStartUrl(startUrl)
                .Build());

            Assert.Contains("Start URL", exception.Message);
        }

        [Fact]
        public void BuildShouldThrowWithMissingOidcRegion()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => PopulateBuilder()
                .WithOidcRegion(null)
                .Build());

            Assert.Contains("OIDC", exception.Message);
        }

        private SonoTokenProviderBuilder PopulateBuilder()
        {
            return SonoTokenProviderBuilder.Create()
                .WithCredentialIdentifier(SonoCredentialId)
                .WithToolkitShell(_toolkitShell.Object);
        }
    }
}
