using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.ResourceFetchers;
using Moq;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.ResourceFetchers
{
    public class ChainedResourceFetcherTests : IDisposable
    {
        private readonly ResourceFetchersFixture _fixture = new ResourceFetchersFixture();
        private readonly Mock<IResourceFetcher> _fetcher1 = new Mock<IResourceFetcher>();
        private readonly Mock<IResourceFetcher> _fetcher2 = new Mock<IResourceFetcher>();
        private readonly Mock<IResourceFetcher> _fetcher3 = new Mock<IResourceFetcher>();
        private readonly string _samplePath = "aaa/bbb/readme.txt";
        private readonly string _sampleData1 = "data 1";
        private readonly string _sampleData2 = "data 2";
        private readonly ChainedResourceFetcher _sut;

        public ChainedResourceFetcherTests()
        {
            _fetcher1.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(_sampleData1)));
            _fetcher2.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(_sampleData2)));
            _fetcher3.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Stream)null);

            _sut = new ChainedResourceFetcher(new List<IResourceFetcher>()
            {
                _fetcher1.Object,
                _fetcher2.Object,
                _fetcher3.Object,
            });
        }

        [Fact]
        public async Task Add()
        {
            var sut = new ChainedResourceFetcher()
                .Add(_fetcher1.Object)
                .Add(_fetcher2.Object)
                .Add(_fetcher3.Object);

            var stream = await sut.GetAsync(_samplePath);
            Assert.NotNull(stream);

            _fetcher1.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Once);
            _fetcher2.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Never);
            _fetcher3.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Never);

            // Did the fixture return the stream contents we expected?
            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_sampleData1, text);
        }

        [Fact]
        public async Task Get_First()
        {
            var stream = await _sut.GetAsync(_samplePath);
            Assert.NotNull(stream);

            _fetcher1.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Once);
            _fetcher2.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Never);
            _fetcher3.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Never);

            // Did the fixture return the stream contents we expected?
            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_sampleData1, text);
        }

        [Fact]
        public async Task Get_Second()
        {
            _fetcher1.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Stream) null);

            var stream = await _sut.GetAsync(_samplePath);
            Assert.NotNull(stream);

            _fetcher1.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Once);
            _fetcher2.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Once);
            _fetcher3.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Never);

            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_sampleData2, text);
        }

        [Fact]
        public async Task Get_FirstFetcherThrows()
        {
            _fetcher1.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Throws(new Exception("simulating inner fetcher error"));

            var stream = await _sut.GetAsync(_samplePath);

            _fetcher1.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Once);
            _fetcher2.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Once);
            _fetcher3.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Never);

            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_sampleData2, text);
        }

        [Fact]
        public async Task Get_AllFetchersThrow()
        {
            _fetcher1.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Throws(new Exception("simulating inner fetcher error"));
            _fetcher2.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Throws(new Exception("simulating inner fetcher error"));
            _fetcher3.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Throws(new Exception("simulating inner fetcher error"));

            Assert.Null(await _sut.GetAsync(_samplePath));

            _fetcher1.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Once);
            _fetcher2.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Once);
            _fetcher3.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Once);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
