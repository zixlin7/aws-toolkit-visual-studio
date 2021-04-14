using System;
using Amazon.AWSToolkit.ResourceFetchers;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.ResourceFetchers
{
    public class AssemblyResourceFetcherTests : IDisposable
    {
        private readonly ResourceFetchersFixture _fixture = new ResourceFetchersFixture();
        private readonly string _samplePath = "endpoints.json";
        private readonly AssemblyResourceFetcher _sut = new AssemblyResourceFetcher();

        [Fact]
        public void Get()
        {
            var stream = _sut.Get(_samplePath);
            Assert.NotNull(stream);

            var text = _fixture.GetStreamContents(stream);
            Assert.NotEmpty(text);
            Assert.Contains("\"description\" : \"Africa (Cape Town)\"", text);
        }

        [Fact]
        public void Get_NoPathExists()
        {
            var stream = _sut.Get($"some-random-file-{Guid.NewGuid()}.txt");
            Assert.Null(stream);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
