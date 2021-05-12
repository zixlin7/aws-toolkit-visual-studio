using System;
using System.IO;
using Amazon.AWSToolkit.ResourceFetchers;
using Amazon.AWSToolkit.Settings;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Tests.Common.Settings;
using Moq;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.ResourceFetchers
{
    public class HostedFilesResourceFetcherTests : IDisposable
    {
        private readonly Mock<ITelemetryLogger> _telemetryLogger = new Mock<ITelemetryLogger>();
        private readonly ResourceFetchersFixture _fixture = new ResourceFetchersFixture();
        private readonly ToolkitSettings _toolkitSettings = FakeToolkitSettings.Create();
        private readonly HostedFilesSettings _hostedFilesSettings;
        private readonly HostedFilesResourceFetcher.Options _options = new HostedFilesResourceFetcher.Options();
        private readonly HostedFilesResourceFetcher _sut;

        private readonly string HostedFilesResourcePath = "VersionInfo.xml";
        private readonly string HostedFilesResourceFragment = "<version number=\"1.18.1.0\"";

        public HostedFilesResourceFetcherTests()
        {
            _hostedFilesSettings = new HostedFilesSettings(_toolkitSettings, _fixture.TestLocation.OutputFolder);
            Directory.CreateDirectory(_hostedFilesSettings.DownloadedCacheFolder);

            _options.LoadFromDownloadCache = true;
            _options.DownloadOncePerSession = false;
            _options.DownloadIfNewer = false;
            _options.TelemetryLogger = _telemetryLogger.Object;
            _sut = new HostedFilesResourceFetcher(_options, _hostedFilesSettings);
        }

        [Fact]
        public void HostedFiles_LocalFolder()
        {
            // Setup: "Local hosted files" contains this sample file
            _fixture.WriteToFile(_fixture.SampleData, _fixture.SampleInputRelativePath);
            _toolkitSettings.HostedFilesLocation = new Uri(_fixture.TestLocation.InputFolder).ToString();

            var stream = _sut.Get(_fixture.SampleRelativePath);
            Assert.NotNull(stream);

            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_fixture.SampleData, text);
        }

        [Fact]
        public void HostedFiles_Url()
        {
            // Setup: "hosted files" explicitly points to the CloudFront distribution
            _toolkitSettings.HostedFilesLocation = S3FileFetcher.CLOUDFRONT_CONFIG_FILES_LOCATION;
            _options.CloudFrontBaseUrl = "";
            _options.S3BaseUrl = "";

            var stream = _sut.Get(HostedFilesResourcePath);
            AssertStreamHasExpectedContents(stream);
            _telemetryLogger.Verify(mock => mock.Record(It.IsAny<Metrics>()), Times.Once);

            // File gets cached
            Assert.True(File.Exists(Path.Combine(_hostedFilesSettings.DownloadedCacheFolder, HostedFilesResourcePath)));
        }

        [Fact]
        public void DownloadCacheEmpty()
        {
            // Setup: try to retrieve a file that isn't in the download cache (or anywhere)
            var stream = _sut.Get("some-file.txt");
            Assert.Null(stream);
        }

        [Fact]
        public void DownloadCacheNotEmpty()
        {
            // Setup: put a file in the download cache that isn't available in any other source
            var resourcePath = "some-file.txt";
            File.WriteAllText(Path.Combine(_hostedFilesSettings.DownloadedCacheFolder, resourcePath), _fixture.SampleData);

            var stream = _sut.Get(resourcePath);
            Assert.NotNull(stream);

            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_fixture.SampleData, text);
        }

        [Fact]
        public void DownloadOncePerSession()
        {
            // Setup: put a file in the download cache that is available in other sources (so that an alternate version is retrieved)
            _options.DownloadOncePerSession = true;
            File.WriteAllText(Path.Combine(_hostedFilesSettings.DownloadedCacheFolder, HostedFilesResourcePath), _fixture.SampleData);

            var stream = _sut.Get(HostedFilesResourcePath);
            AssertStreamHasExpectedContents(stream);
            _telemetryLogger.Verify(mock => mock.Record(It.IsAny<Metrics>()), Times.Once);
        }

        [Fact]
        public void ValidationFails()
        {
            // Setup: put a file in the download cache that is known to have a fallback in-assembly.
            // Then have it fail the validation check so that the in-assembly resource is used.
            _options.ResourceValidator = s => false;
            _options.CloudFrontBaseUrl = "";
            _options.S3BaseUrl = "";

            File.WriteAllText(Path.Combine(_hostedFilesSettings.DownloadedCacheFolder, HostedFilesResourcePath), _fixture.SampleData);

            var stream = _sut.Get("endpoints.json");

            Assert.NotNull(stream);
            var text = _fixture.GetStreamContents(stream);
            Assert.Contains("\"description\" : \"Africa (Cape Town)\"", text);
            _telemetryLogger.Verify(mock => mock.Record(It.IsAny<Metrics>()), Times.Never);
        }

        public void Dispose()
        {
            _fixture?.Dispose();
        }

        private void AssertStreamHasExpectedContents(Stream stream)
        {
            Assert.NotNull(stream);

            var text = _fixture.GetStreamContents(stream);
            Assert.Contains(HostedFilesResourceFragment, text);
        }
    }
}
