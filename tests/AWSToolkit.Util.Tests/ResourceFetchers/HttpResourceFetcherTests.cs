using System;
using System.IO;
using System.Threading.Tasks;

using Amazon.AWSToolkit.ResourceFetchers;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Moq;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.ResourceFetchers
{
    /// <summary>
    /// These tests are more integration in nature in that they make outbound http requests
    /// </summary>
    public class HttpResourceFetcherTests
    {
        private readonly string _baseUrl = S3FileFetcher.HOSTEDFILES_LOCATION;
        private readonly Mock<ITelemetryLogger> _telemetryLogger = new Mock<ITelemetryLogger>();
        private readonly HttpResourceFetcherOptions _fetcherOptions = new HttpResourceFetcherOptions();
        private readonly HttpResourceFetcher _sut;

        public HttpResourceFetcherTests()
        {
            _fetcherOptions.TelemetryLogger = _telemetryLogger.Object;
            _sut = new HttpResourceFetcher(_fetcherOptions);
        }

        [Fact]
        public async Task Get_ValidUrl()
        {
            var url = $"{_baseUrl}VersionInfo.xml";
            var stream = await _sut.GetAsync(url);

            Assert.NotNull(stream);

            using (var reader = new StreamReader(stream))
            {
                var text = await reader.ReadToEndAsync();
                Assert.NotEmpty(text);
            }

            _telemetryLogger.Verify(mock => mock.Record(It.IsAny<Metrics>()), Times.Once);
        }

        [Fact]
        public async Task Get_NoUrlExists()
        {
            var url = $"{_baseUrl}random-file-{Guid.NewGuid()}.xml";
            Assert.Null(await _sut.GetAsync(url));
        }
    }
}
