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
        private static readonly ICredentialIdentifier SonoCredentialId = new SonoCredentialIdentifier("sono");
        
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
        public void BuildShowThrowWithMissingToolkitShell()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => PopulateBuilder()
                .WithToolkitShell(null)
                .Build());

            Assert.Contains("Toolkit shell", exception.Message);
        }

        [Fact]
        public void BuildShowThrowWithMissingCredentialId()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => PopulateBuilder()
                .WithCredentialIdentifier(null)
                .Build());

            Assert.Contains("Credential", exception.Message);
        }

        [Fact]
        public void BuildShowThrowWithMissingSessionName()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => PopulateBuilder()
                .WithSessionName(null)
                .Build());

            Assert.Contains("Session", exception.Message);
        }

        [Fact]
        public void BuildShowThrowWithMissingProviderRegion()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => PopulateBuilder()
                .WithTokenProviderRegion(null)
                .Build());

            Assert.Contains("Token Provider", exception.Message);
        }

        [Fact]
        public void BuildShowThrowWithMissingOidcRegion()
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
