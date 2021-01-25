using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            _fetcher1.Setup(mock => mock.Get(It.IsAny<string>())).Returns<string>((path) =>
                new MemoryStream(Encoding.UTF8.GetBytes(_sampleData1)));
            _fetcher2.Setup(mock => mock.Get(It.IsAny<string>())).Returns<string>((path) =>
                new MemoryStream(Encoding.UTF8.GetBytes(_sampleData2)));
            _fetcher3.Setup(mock => mock.Get(It.IsAny<string>())).Returns<string>((path) => null);

            _sut = new ChainedResourceFetcher(new List<IResourceFetcher>()
            {
                _fetcher1.Object,
                _fetcher2.Object,
                _fetcher3.Object,
            });
        }

        [Fact]
        public void Add()
        {
            var sut = new ChainedResourceFetcher()
                .Add(_fetcher1.Object)
                .Add(_fetcher2.Object)
                .Add(_fetcher3.Object);

            var stream = sut.Get(_samplePath);
            Assert.NotNull(stream);

            _fetcher1.Verify(mock => mock.Get(_samplePath), Times.Once);
            _fetcher2.Verify(mock => mock.Get(_samplePath), Times.Never);
            _fetcher3.Verify(mock => mock.Get(_samplePath), Times.Never);

            // Did the fixture return the stream contents we expected?
            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_sampleData1, text);
        }

        [Fact]
        public void Get_First()
        {
            var stream = _sut.Get(_samplePath);
            Assert.NotNull(stream);

            _fetcher1.Verify(mock => mock.Get(_samplePath), Times.Once);
            _fetcher2.Verify(mock => mock.Get(_samplePath), Times.Never);
            _fetcher3.Verify(mock => mock.Get(_samplePath), Times.Never);

            // Did the fixture return the stream contents we expected?
            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_sampleData1, text);
        }

        [Fact]
        public void Get_Second()
        {
            _fetcher1.Setup(mock => mock.Get(It.IsAny<string>())).Returns<string>((path) => null);

            var stream = _sut.Get(_samplePath);
            Assert.NotNull(stream);

            _fetcher1.Verify(mock => mock.Get(_samplePath), Times.Once);
            _fetcher2.Verify(mock => mock.Get(_samplePath), Times.Once);
            _fetcher3.Verify(mock => mock.Get(_samplePath), Times.Never);

            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_sampleData2, text);
        }

        [Fact]
        public void Get_FirstFetcherThrows()
        {
            _fetcher1.Setup(mock => mock.Get(It.IsAny<string>())).Throws(new Exception("simulating inner fetcher error"));

            var stream = _sut.Get(_samplePath);

            _fetcher1.Verify(mock => mock.Get(_samplePath), Times.Once);
            _fetcher2.Verify(mock => mock.Get(_samplePath), Times.Once);
            _fetcher3.Verify(mock => mock.Get(_samplePath), Times.Never);

            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_sampleData2, text);
        }

        [Fact]
        public void Get_AllFetchersThrow()
        {
            _fetcher1.Setup(mock => mock.Get(It.IsAny<string>())).Throws(new Exception("simulating inner fetcher error"));
            _fetcher2.Setup(mock => mock.Get(It.IsAny<string>())).Throws(new Exception("simulating inner fetcher error"));
            _fetcher3.Setup(mock => mock.Get(It.IsAny<string>())).Throws(new Exception("simulating inner fetcher error"));

            Assert.Null(_sut.Get(_samplePath));

            _fetcher1.Verify(mock => mock.Get(_samplePath), Times.Once);
            _fetcher2.Verify(mock => mock.Get(_samplePath), Times.Once);
            _fetcher3.Verify(mock => mock.Get(_samplePath), Times.Once);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
