using System;
using System.IO;
using System.Text;
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
        public void Get()
        {
            _fetcher.Setup(mock => mock.Get(It.IsAny<string>())).Returns<string>((path) =>
                new MemoryStream(Encoding.UTF8.GetBytes(_fixture.SampleData)));

            var stream = _sut.Get(_samplePath);
            Assert.NotNull(stream);

            _fetcher.Verify(mock => mock.Get(_samplePath), Times.Once);

            // Did the fixture return the stream contents we expected?
            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_fixture.SampleData, text);

            // Did the fixture write to cache?
            var cachedFilePath = _fnGetCachePath(_samplePath);
            Assert.True(File.Exists(cachedFilePath));
            Assert.Equal(_fixture.SampleData, File.ReadAllText(cachedFilePath));
        }

        [Fact]
        public void Get_FetcherHasNoData()
        {
            _fetcher.Setup(mock => mock.Get(It.IsAny<string>())).Returns<string>(null);
            var stream = _sut.Get(_samplePath);
            Assert.Null(stream);
        }

        [Fact]
        public void Get_FetcherThrows()
        {
            _fetcher.Setup(mock => mock.Get(It.IsAny<string>())).Throws(new Exception("simulating inner fetcher error"));
            var stream = _sut.Get(_samplePath);
            Assert.Null(stream);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
