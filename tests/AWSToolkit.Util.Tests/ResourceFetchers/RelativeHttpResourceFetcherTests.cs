using System;
using System.IO;
using Amazon.AWSToolkit.ResourceFetchers;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.ResourceFetchers
{
    /// <summary>
    /// These tests are more integration in nature in that they make outbound http requests
    /// </summary>
    public class RelativeHttpResourceFetcherTests
    {
        private readonly string _baseUrl = S3FileFetcher.CLOUDFRONT_CONFIG_FILES_LOCATION;
        private readonly RelativeHttpResourceFetcher _sut;

        public RelativeHttpResourceFetcherTests()
        {
            _sut = new RelativeHttpResourceFetcher(new RelativeHttpResourceFetcher.Options()
            {
                BasePath = _baseUrl,
            });
        }

        [Fact]
        public void Get_ValidUrl()
        {
            var url = "ServiceEndPoints.xml";
            var stream = _sut.Get(url);

            Assert.NotNull(stream);

            using (var reader = new StreamReader(stream))
            {
                var text = reader.ReadToEnd();
                Assert.NotEmpty(text);
            }
        }

        [Fact]
        public void Get_NoUrlExists()
        {
            var url = $"random-file-{Guid.NewGuid()}.xml";
            Assert.Null(_sut.Get(url));
        }
    }
}
