using System;
using System.Threading.Tasks;

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
        public async Task Get()
        {
            var stream = await _sut.GetAsync(_samplePath);
            Assert.NotNull(stream);

            var text = _fixture.GetStreamContents(stream);
            Assert.NotEmpty(text);
            Assert.Contains("\"description\" : \"Africa (Cape Town)\"", text);
        }

        [Fact]
        public async Task Get_NoPathExists()
        {
            var stream = await _sut.GetAsync($"some-random-file-{Guid.NewGuid()}.txt");
            Assert.Null(stream);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
