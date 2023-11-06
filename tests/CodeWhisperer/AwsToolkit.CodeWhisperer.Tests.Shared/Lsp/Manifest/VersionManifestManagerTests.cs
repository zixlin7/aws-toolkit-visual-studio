using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Install;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;
using Amazon.AWSToolkit.ResourceFetchers;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Common.Context;

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
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly VersionManifestManager _sut;
        private MetricDatum _metric;

        public VersionManifestManagerTests()
        {
            SetupFetcher(_validManifestFileName);
            var options = new VersionManifestOptions()
            {
                FileName = _validManifestFileName,
                MajorVersion = CodeWhispererConstants.ManifestCompatibleMajorVersion,
                ToolkitContext = _contextFixture.ToolkitContext
            };
            _contextFixture.SetupTelemetryCallback(metrics =>
            {
                var datum = metrics.Data.FirstOrDefault(x => string.Equals(x.MetricName, "languageServer_setup"));
                if (datum != null)
                {
                    _metric = datum;
                }
            });
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

            AssertLspSetupTelemetryResult(Result.Cancelled);
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

            AssertLspSetupTelemetryResult(Result.Succeeded);
        }

        [Fact]
        public async Task Download_WhenInvalidSchemaAsync()
        {
            SetupFetcher(_invalidManifestFileName);
            await Assert.ThrowsAsync<JsonSerializationException>(async () => await _sut.DownloadAsync());

            _sampleManifestFetcher.Verify(mock => mock.GetAsync(
                _validManifestFileName, It.IsAny<CancellationToken>()), Times.Exactly(1));

            AssertLspSetupTelemetryResult(Result.Failed);
        }

        [Fact]
        public async Task Download_WhenEmptySchemaAsync()
        {
            SetupFetcher("non-existent.json");
            var exception = await Assert.ThrowsAsync<LspToolkitException>(async () => await _sut.DownloadAsync());

            _sampleManifestFetcher.Verify(mock => mock.GetAsync(
                _validManifestFileName, It.IsAny<CancellationToken>()), Times.Exactly(1));
            Assert.Equal(exception.Code, LspToolkitException.LspErrorCode.UnexpectedManifestFetchError.ToString());

            AssertLspSetupTelemetryResult(Result.Failed);
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

            AssertLspSetupTelemetryResult(Result.Failed);
        }

        private void AssertLspSetupTelemetryResult(Result result)
        {
            Assert.Equal(result.ToString(), _metric.Metadata["result"]);
            Assert.Equal(LanguageServerSetupStage.GetManifest.ToString(), _metric.Metadata["languageServerSetupStage"]);
        }
    }
}
