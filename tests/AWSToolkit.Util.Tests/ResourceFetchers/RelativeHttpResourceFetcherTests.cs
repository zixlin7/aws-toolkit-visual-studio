using System;
using System.IO;
using System.Threading.Tasks;

using Amazon.AWSToolkit.ResourceFetchers;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.ResourceFetchers
{
    /// <summary>
    /// These tests are more integration in nature in that they make outbound http requests
    /// </summary>
    public class RelativeHttpResourceFetcherTests
    {
        private readonly string _baseUrl = S3FileFetcher.HOSTEDFILES_LOCATION;
        private readonly RelativeHttpResourceFetcher _sut;

        public RelativeHttpResourceFetcherTests()
        {
            _sut = new RelativeHttpResourceFetcher(new RelativeHttpResourceFetcher.Options()
            {
                BasePath = _baseUrl,
            });
        }

        [Fact]
        public async Task Get_ValidUrl()
        {
            var url = "VersionInfo.xml";
            var stream = await _sut.GetAsync(url);

            Assert.NotNull(stream);

            using (var reader = new StreamReader(stream))
            {
                var text = await reader.ReadToEndAsync();
                Assert.NotEmpty(text);
            }
        }

        [Fact]
        public async Task Get_NoUrlExists()
        {
            var url = $"random-file-{Guid.NewGuid()}.xml";
            Assert.Null(await _sut.GetAsync(url));
        }
    }
}
