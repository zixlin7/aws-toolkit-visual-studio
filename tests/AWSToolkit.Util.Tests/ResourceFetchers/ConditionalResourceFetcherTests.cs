using System;
using System.IO;
using System.Text;
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
        private readonly Func<Stream, bool> _successConditional = stream =>
        {
            stream.Dispose();
            return true;
        };
        private readonly Func<Stream, bool> _failConditional = stream =>
        {
            stream.Dispose();
            return false;
        };
        private readonly string _samplePath = "aaa/bbb/readme.txt";
        private ConditionalResourceFetcher _sut;

        public ConditionalResourceFetcherTests()
        {
            _fetcher.Setup(mock => mock.Get(It.IsAny<string>())).Returns<string>((path) =>
                new MemoryStream(Encoding.UTF8.GetBytes(_fixture.SampleData)));

            _sut = new ConditionalResourceFetcher(_fetcher.Object, _successConditional);
        }

        [Fact]
        public void Get_ConditionalSuccess()
        {
            var stream = _sut.Get(_samplePath);
            Assert.NotNull(stream);

            _fetcher.Verify(mock => mock.Get(_samplePath), Times.Once);

            // Did the fixture return the stream contents we expected?
            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_fixture.SampleData, text);
        }

        [Fact]
        public void Get_ConditionalFailure()
        {
            _sut = new ConditionalResourceFetcher(_fetcher.Object, _failConditional);

            var stream = _sut.Get(_samplePath);
            Assert.Null(stream);
            _fetcher.Verify(mock => mock.Get(_samplePath), Times.Once);
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
