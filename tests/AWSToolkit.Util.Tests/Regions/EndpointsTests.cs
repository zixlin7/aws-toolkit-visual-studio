using Amazon.AWSToolkit.Regions.Manifest;
using Amazon.AWSToolkit.Util.Tests.Resources;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Regions
{
    public class EndpointsTests
    {
        private const string EndpointsFilename = "sample-endpoints.json";

        [Fact]
        public void LoadStream()
        {
            using (var stream = TestResources.LoadResourceFile(EndpointsFilename))
            {
                Assert.NotNull(stream);
                var sut = Endpoints.Load(stream);

                Assert.NotNull(sut);
                Assert.Equal("3", sut.Version);
                Assert.NotEmpty(sut.Partitions);
            }
        }
    }
}
