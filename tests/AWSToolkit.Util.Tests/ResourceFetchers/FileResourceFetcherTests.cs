using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.ResourceFetchers;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.ResourceFetchers
{
    public class FileResourceFetcherTests : IDisposable
    {
        private readonly ResourceFetchersFixture _fixture = new ResourceFetchersFixture();
        private readonly FileResourceFetcher _sut = new FileResourceFetcher();
        private readonly string _sampleFileRelativePath = "readme.txt";

        public FileResourceFetcherTests()
        {
            _fixture.WriteToFile(_fixture.SampleData, _sampleFileRelativePath);
        }

        [Fact]
        public async Task Get_FileExists()
        {
            var stream = await _sut.GetAsync(_fixture.GetFullPath(_sampleFileRelativePath));
            Assert.NotNull(stream);

            var text = _fixture.GetStreamContents(stream);
            Assert.Equal(_fixture.SampleData, text);
        }

        [Fact]
        public async Task Get_NoFileExists()
        {
            var stream = await _sut.GetAsync(_fixture.GetFullPath("bees.txt"));
            Assert.Null(stream);
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
