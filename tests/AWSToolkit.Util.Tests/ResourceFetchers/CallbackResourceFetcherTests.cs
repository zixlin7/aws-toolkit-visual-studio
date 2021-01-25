using System;
using System.IO;
using System.Text;
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
                return stream;
            };

            _fetcher.Setup(mock => mock.Get(It.IsAny<string>())).Returns<string>((path) =>
                new MemoryStream(Encoding.UTF8.GetBytes(_fixture.SampleData)));

            _sut = new CallbackResourceFetcher(_fetcher.Object, _callback);
        }

        [Fact]
        public void Get_First()
        {
            var stream = _sut.Get(_samplePath);
            Assert.NotNull(stream);

            Assert.Equal(1, _callbackTimesCalled);

            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_fixture.SampleData, text);
        }

        [Fact]
        public void Get_FetcherThrows()
        {
            _fetcher.Setup(mock => mock.Get(It.IsAny<string>()))
                .Throws(new Exception("simulating inner fetcher error"));

            var stream = _sut.Get(_samplePath);

            Assert.Null(stream);
            Assert.Equal(0, _callbackTimesCalled);
        }

        [Fact]
        public void Get_CallbackReturnsNull()
        {
            var sut = new CallbackResourceFetcher(_fetcher.Object, (text, stream) => null);

            Assert.Null(sut.Get(_samplePath));
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
