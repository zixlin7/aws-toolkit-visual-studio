﻿using System;
using System.IO;
using System.Linq;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.IO;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Sono
{
    public class SonoCredentialProviderFactoryTests : IDisposable
    {
        private static readonly ToolkitRegion SampleRegion = new ToolkitRegion()
        {
            DisplayName = "sample-region",
            Id = "sample-region",
        };

        private static readonly ICredentialIdentifier SonoCredentialId =
            new SonoCredentialIdentifier(SonoCredentialProviderFactory.CodeCatalystProfileName);
        private static readonly ICredentialIdentifier OtherSonoCredentialId = new SonoCredentialIdentifier("other-sono");
        private static readonly ICredentialIdentifier NonSonoCredentialId = new SDKCredentialIdentifier("non-sono-sample");
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation(false);
        private readonly Mock<IAWSToolkitShellProvider> _toolkitShell = new Mock<IAWSToolkitShellProvider>();
        private readonly SonoCredentialProviderFactory _sut;

        public SonoCredentialProviderFactoryTests()
        {
            _sut = new SonoCredentialProviderFactory(_toolkitShell.Object, _testLocation.TestFolder);
            _sut.Initialize();
        }

        [Theory]
        [InlineData(SonoCredentialProviderFactory.CodeCatalystProfileName)]
        [InlineData(SonoCredentialProviderFactory.CodeWhispererProfileName)]
        public void GetCredentialIdentifiers(string profileName)
        {
            var ids = _sut.GetCredentialIdentifiers();

            Assert.Equal(2, ids.Count);

            var id = ids.FirstOrDefault(i => i.ProfileName == profileName);

            Assert.NotNull(id);
            Assert.Equal(SonoCredentialId.FactoryId, id.FactoryId);
            Assert.Equal($"{SonoCredentialProviderFactory.FactoryId}:{profileName}", id.Id);
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

        /// <summary>
        /// This test checks that <see cref="ICredentialSettingsManagerExtensionMethods.GetCredentialType"/>
        /// will produce the expected result.
        /// </summary>
        [Fact]
        public void ProducesBearerTokenCredentialType()
        {
            Assert.Equal(CredentialType.BearerToken,
                _sut.GetCredentialProfileProcessor().GetProfileProperties(SonoCredentialId).GetCredentialType());
        }

        [Fact]
        public void Invalidate()
        {
            var ssoSession = _sut.GetCredentialProfileProcessor().GetProfileProperties(SonoCredentialId).SsoSession;

            var cachePath = Path.Combine(
                _testLocation.TestFolder,
                TokenCache.GetCacheFilename(SonoProperties.StartUrl, ssoSession));

            File.WriteAllText(cachePath, "a sample file that will be deleted");

            _sut.Invalidate(SonoCredentialId);

            Assert.False(File.Exists(cachePath));
        }

        [Fact]
        public void InvalidateShouldThrowOnDifferentId()
        {
            Assert.Throws<NotSupportedException>(() => _sut.Invalidate(OtherSonoCredentialId));
        }

        public void Dispose()
        {
            _testLocation?.Dispose();
        }
    }
}
