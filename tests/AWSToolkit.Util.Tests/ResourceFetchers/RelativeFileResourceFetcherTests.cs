using System;
using System.IO;
using System.Threading.Tasks;

using Amazon.AWSToolkit.ResourceFetchers;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.ResourceFetchers
{
    public class RelativeFileResourceFetcherTests : IDisposable
    {
        private readonly ResourceFetchersFixture _fixture = new ResourceFetchersFixture();
        private readonly RelativeFileResourceFetcher _sut;
        private readonly string _sampleFileRelativePath = "aaa/bbb/readme.txt";

        public RelativeFileResourceFetcherTests()
        {
            _sut = new RelativeFileResourceFetcher(_fixture.TestLocation.TestFolder);

            _fixture.WriteToFile(_fixture.SampleData, _sampleFileRelativePath);
        }

        [Fact]
        public async Task Get_FileExists()
        {
            await AssertGetReturnsSampleData(new RelativeFileResourceFetcher(_fixture.TestLocation.TestFolder));
            await AssertGetReturnsSampleData(new RelativeFileResourceFetcher(_fixture.TestLocation.TestFolder + "/"));
        }

        [Fact]
        public async Task Get_NoFileExists()
        {
            var stream = await _sut.GetAsync("aaa/bbb/bees.txt");
            Assert.Null(stream);
        }

        private async Task AssertGetReturnsSampleData(RelativeFileResourceFetcher sut)
        {
            var stream = await sut.GetAsync(_sampleFileRelativePath);
            Assert.NotNull(stream);

            using (var reader = new StreamReader(stream))
            {
                var text = await reader.ReadToEndAsync();
                Assert.Equal(_fixture.SampleData, text);
            }
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
