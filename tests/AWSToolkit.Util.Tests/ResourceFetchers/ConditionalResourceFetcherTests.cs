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
    public class ConditionalResourceFetcherTests : IDisposable
    {
        private readonly ResourceFetchersFixture _fixture = new ResourceFetchersFixture();
        private readonly Mock<IResourceFetcher> _fetcher = new Mock<IResourceFetcher>();
        // We also simulate the conditional consuming and disposing the stream, which
        // would happen with StreamReaders.
        private readonly Func<Stream, Task<bool>> _successConditional = stream =>
        {
            stream.Dispose();
            return Task.FromResult(true);
        };
        private readonly Func<Stream, Task<bool>> _failConditional = stream =>
        {
            stream.Dispose();
            return Task.FromResult(false);
        };
        private readonly string _samplePath = "aaa/bbb/readme.txt";
        private ConditionalResourceFetcher _sut;

        public ConditionalResourceFetcherTests()
        {
            _fetcher.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(_fixture.SampleData)));

            _sut = new ConditionalResourceFetcher(_fetcher.Object, _successConditional);
        }

        [Fact]
        public async Task Get_ConditionalSuccess()
        {
            var stream = await _sut.GetAsync(_samplePath);
            Assert.NotNull(stream);

            _fetcher.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Once);

            // Did the fixture return the stream contents we expected?
            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_fixture.SampleData, text);
        }

        [Fact]
        public async Task Get_ConditionalFailure()
        {
            _sut = new ConditionalResourceFetcher(_fetcher.Object, _failConditional);

            var stream = await _sut.GetAsync(_samplePath);
            Assert.Null(stream);
            _fetcher.Verify(mock => mock.GetAsync(_samplePath, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Get_FetcherHasNoData()
        {
            _fetcher.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Stream)null);
            var stream = await _sut.GetAsync(_samplePath);
            Assert.Null(stream);
        }

        [Fact]
        public async Task Get_FetcherThrows()
        {
            _fetcher.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("simulating inner fetcher error"));
            var stream = await _sut.GetAsync(_samplePath);
            Assert.Null(stream);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
