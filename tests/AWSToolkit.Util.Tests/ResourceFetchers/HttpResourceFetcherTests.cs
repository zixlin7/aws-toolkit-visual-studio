using System;
using System.IO;
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
        private readonly string _baseUrl = S3FileFetcher.CLOUDFRONT_CONFIG_FILES_LOCATION;
        private readonly Mock<ITelemetryLogger> _telemetryLogger = new Mock<ITelemetryLogger>();
        private readonly HttpResourceFetcherOptions _fetcherOptions = new HttpResourceFetcherOptions();
        private readonly HttpResourceFetcher _sut;

        public HttpResourceFetcherTests()
        {
            _fetcherOptions.TelemetryLogger = _telemetryLogger.Object;
            _sut = new HttpResourceFetcher(_fetcherOptions);
        }

        [Fact]
        public void Get_ValidUrl()
        {
            var url = $"{_baseUrl}ServiceEndPoints.xml";
            var stream = _sut.Get(url);

            Assert.NotNull(stream);

            using (var reader = new StreamReader(stream))
            {
                var text = reader.ReadToEnd();
                Assert.NotEmpty(text);
            }

            _telemetryLogger.Verify(mock => mock.Record(It.IsAny<Metrics>()), Times.Once);
        }

        [Fact]
        public void Get_NoUrlExists()
        {
            var url = $"{_baseUrl}random-file-{Guid.NewGuid()}.xml";
            Assert.Null(_sut.Get(url));
        }
    }
}
