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
    public class CallbackResourceFetcherTests : IDisposable
    {
        private readonly ResourceFetchersFixture _fixture = new ResourceFetchersFixture();
        private readonly Mock<IResourceFetcher> _fetcher = new Mock<IResourceFetcher>();
        private readonly string _samplePath = "aaa/bbb/readme.txt";
        private readonly CallbackResourceFetcher _sut;

        private int _callbackTimesCalled = 0;
        private readonly CallbackResourceFetcher.PostProcessStream _callback;

        public CallbackResourceFetcherTests()
        {
            _callback = (text, stream) =>
            {
                _callbackTimesCalled++;
                return Task.FromResult(stream);
            };

            _fetcher.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(_fixture.SampleData)));

            _sut = new CallbackResourceFetcher(_fetcher.Object, _callback);
        }

        [Fact]
        public async Task Get_First()
        {
            var stream = await _sut.GetAsync(_samplePath);
            Assert.NotNull(stream);

            Assert.Equal(1, _callbackTimesCalled);

            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_fixture.SampleData, text);
        }

        [Fact]
        public async Task Get_FetcherThrows()
        {
            _fetcher.Setup(mock => mock.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("simulating inner fetcher error"));

            var stream = await _sut.GetAsync(_samplePath);

            Assert.Null(stream);
            Assert.Equal(0, _callbackTimesCalled);
        }

        [Fact]
        public async Task Get_CallbackReturnsNull()
        {
            var sut = new CallbackResourceFetcher(_fetcher.Object, (text, stream) => null);

            Assert.Null(await sut.GetAsync(_samplePath));
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
