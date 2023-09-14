using System.Threading.Tasks;

using Amazon.AWSToolkit.Regions.Manifest;
using Amazon.AWSToolkit.Util.Tests.Resources;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Regions
{
    public class EndpointsTests
    {
        private const string EndpointsFilename = "sample-endpoints.json";

        [Fact]
        public async Task LoadStream()
        {
            using (var stream = TestResources.LoadResourceFile(EndpointsFilename))
            {
                Assert.NotNull(stream);
                var sut = await Endpoints.LoadAsync(stream);

                Assert.NotNull(sut);
                Assert.Equal("3", sut.Version);
                Assert.NotEmpty(sut.Partitions);
            }
        }
    }
}
