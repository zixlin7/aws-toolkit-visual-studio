using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.ResourceFetchers;
using Moq;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.ResourceFetchers
{
    public class CachingResourceFetcherTests : IDisposable
    {
        private readonly ResourceFetchersFixture _fixture = new ResourceFetchersFixture();
        private readonly Mock<IResourceFetcher> _fetcher = new Mock<IResourceFetcher>();
        private readonly CachingResourceFetcher.GetCacheFullPath _fnGetCachePath;
        private readonly string _samplePath = "aaa/bbb/readme.txt";
        private readonly CachingResourceFetcher _sut;

        public CachingResourceFetcherTests()
        {
            _fnGetCachePath = relativePath => Path.Combine(_fixture.TestLocation.OutputFolder, relativePath);
            _sut = new CachingResourceFetcher(_fetcher.Object, _fnGetCachePath);
        }

        [Fact]
        public async Task Get()
        {
            _fetcher.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(_fixture.SampleData)));

            var stream = await _sut.GetAsync(_samplePath);
            Assert.NotNull(stream);

            _fetcher.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Once);

            // Did the fixture return the stream contents we expected?
            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_fixture.SampleData, text);

            // Did the fixture write to cache?
            var cachedFilePath = _fnGetCachePath(_samplePath);
            Assert.True(File.Exists(cachedFilePath));
            Assert.Equal(_fixture.SampleData, File.ReadAllText(cachedFilePath));
        }

        [Fact]
        public async Task Get_FetcherHasNoData()
        {
            _fetcher.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns<string>(null);
            var stream = await _sut.GetAsync(_samplePath);
            Assert.Null(stream);
        }

        [Fact]
        public async Task Get_FetcherThrows()
        {
            _fetcher.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Throws(new Exception("simulating inner fetcher error"));
            var stream = await _sut.GetAsync(_samplePath);
            Assert.Null(stream);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
