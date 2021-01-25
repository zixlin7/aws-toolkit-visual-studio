using System;
using System.IO;
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
        public void Get_FileExists()
        {
            AssertGetReturnsSampleData(new RelativeFileResourceFetcher(_fixture.TestLocation.TestFolder));
            AssertGetReturnsSampleData(new RelativeFileResourceFetcher(_fixture.TestLocation.TestFolder + "/"));
        }

        [Fact]
        public void Get_NoFileExists()
        {
            var stream = _sut.Get("aaa/bbb/bees.txt");
            Assert.Null(stream);
        }

        private void AssertGetReturnsSampleData(RelativeFileResourceFetcher sut)
        {
            var stream = sut.Get(_sampleFileRelativePath);
            Assert.NotNull(stream);

            using (var reader = new StreamReader(stream))
            {
                var text = reader.ReadToEnd();
                Assert.Equal(_fixture.SampleData, text);
            }
        }

        public void Dispose()
        {
            _fixture.Dispose();
        }
    }
}
