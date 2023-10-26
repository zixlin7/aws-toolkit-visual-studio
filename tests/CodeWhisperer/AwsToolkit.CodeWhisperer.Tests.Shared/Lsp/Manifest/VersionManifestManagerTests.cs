using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.ResourceFetchers;

using Moq;

using Newtonsoft.Json;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Manifest
{
    public class VersionManifestManagerTests
    {
        private const string _validManifestFileName = "sample-manifest.json";
        private const string _invalidManifestFileName = "sample-invalid-manifest.json";
        private readonly Mock<IResourceFetcher> _sampleManifestFetcher = new Mock<IResourceFetcher>();
        private readonly VersionManifestManager _sut;

        public VersionManifestManagerTests()
        {
            SetupFetcher(_validManifestFileName);
            var options = new VersionManifestOptions()
            {
                FileName = _validManifestFileName,
                MajorVersion = CodeWhispererConstants.ManifestCompatibleMajorVersion
            };
            _sut = new VersionManifestManager(options, _sampleManifestFetcher.Object);
        }

        private void SetupFetcher(string fileName)
        {
            _sampleManifestFetcher.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => TestResources.LoadResourceFile(fileName));
        }

        [Fact]
        public async Task DownloadAsync_WhenCancelled()
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await _sut.DownloadAsync(tokenSource.Token));

            _sampleManifestFetcher.Verify(mock => mock.GetAsync(
                _validManifestFileName, It.IsAny<CancellationToken>()), Times.Exactly(0));
        }

        [Fact]
        public async Task DownloadAsync()
        {
            SetupFetcher(_validManifestFileName);
            var manifestSchema = await _sut.DownloadAsync();

            _sampleManifestFetcher.Verify(mock => mock.GetAsync(
                _validManifestFileName, It.IsAny<CancellationToken>()), Times.Exactly(1));

            Assert.NotNull(manifestSchema);
            Assert.Equal("0.1", manifestSchema.ManifestSchemaVersion);
            Assert.Equal(3, manifestSchema.Versions.Count);
        }

        [Fact]
        public async Task Download_WhenInvalidSchemaAsync()
        {
            SetupFetcher(_invalidManifestFileName);
            await Assert.ThrowsAsync<JsonSerializationException>(async () => await _sut.DownloadAsync());

            _sampleManifestFetcher.Verify(mock => mock.GetAsync(
                _validManifestFileName, It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public async Task Download_WhenEmptySchemaAsync()
        {
            SetupFetcher("non-existent.json");
            var exception = await Assert.ThrowsAsync<ToolkitException>(async () => await _sut.DownloadAsync());

            _sampleManifestFetcher.Verify(mock => mock.GetAsync(
                _validManifestFileName, It.IsAny<CancellationToken>()), Times.Exactly(1));
            Assert.Equal(exception.Code, ToolkitException.CommonErrorCode.UnsupportedState.ToString());
        }


        [Fact]
        public async Task Download_WhenFetcherRaisesExceptionAsync()
        {
            _sampleManifestFetcher.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("test error"));

            var exception = await Assert.ThrowsAsync<Exception>(async () => await _sut.DownloadAsync());

            _sampleManifestFetcher.Verify(mock => mock.GetAsync(
                _validManifestFileName, It.IsAny<CancellationToken>()), Times.Exactly(1));
            Assert.Equal("test error", exception.Message);
        }
    }
}
