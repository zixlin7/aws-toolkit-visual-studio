using Amazon.AWSToolkit.Publish.NuGet;
using Amazon.AWSToolkit.Publish.Util;

using Xunit;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.Tests.Integration.NuGet
{
    public class NuGetRepositoryTests
    {
        private readonly NuGetRepository _repository;

        public NuGetRepositoryTests()
        {
            _repository = new NuGetRepository();
        }

        [Theory]
        [InlineData("Newtonsoft.Json", "12.0.*", "12.0.3")]
        [InlineData("aws.deploy.cli", "0.10.*", "0.10.6")]
        [InlineData("aws.deploy.cli", "[0.9, 0.10]", "0.9.7")]
        [InlineData("aws.deploy.cli", "0.10.6", "0.10.6")]
        [InlineData(PublishToAwsConstants.DeployToolPackageName, "0.41.*", "0.41.4")]
        [InlineData(PublishToAwsConstants.DeployToolPackageName, "[0.40, 0.42]", "0.41.4")]
        [InlineData(PublishToAwsConstants.DeployToolPackageName, "0.41.4", "0.41.4")]
        public async Task ShouldGetBestVersionInRange(string package, string versionRange, string expectedVersion)
        {
            // act.
            var version = await _repository.GetBestVersionInRangeAsync(package, versionRange);

            // assert.
            Assert.Equal(expectedVersion, version.ToString());
        }

        [Fact]
        public async Task ShouldThrowIfNoVersionFound()
        {
            await Assert.ThrowsAsync<NoVersionFoundException>(() => _repository.GetBestVersionInRangeAsync(PublishToAwsConstants.DeployToolPackageName, "123.123.123"));
        }
    }
}
