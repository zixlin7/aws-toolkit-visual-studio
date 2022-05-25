using Amazon.AWSToolkit.Publish.Models;

using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class TargetSystemCapabilityTests
    {
        [Fact]
        public void ShouldConvert()
        {
            //arrange
            var summary = CreateSampleSummary();

            //act
            var capability = new TargetSystemCapability(summary);

            //assert
            AssertSummaryEqualsCapability(summary, capability);
        }

        private SystemCapabilitySummary CreateSampleSummary(string url = "https://www.amazon.com")
        {
            return new SystemCapabilitySummary()
            {
                Name = "Docker",
                Message = "Docker must be installed to publish application",
                InstallationUrl = url
            };
        }

        private void AssertSummaryEqualsCapability(SystemCapabilitySummary summary, TargetSystemCapability capability)
        {
            Assert.Equal(summary.Name, capability.Name);
            Assert.Equal(summary.Message, capability.Message);
            Assert.Equal(summary.InstallationUrl, capability.InstallationUrl);
        }

        [Theory]
        [InlineData("http://www.amazon.com")]
        [InlineData("https://www.amazon.com")]
        public void ShouldHaveUrl(string url)
        {
            //arrange
            var summary = CreateSampleSummary(url);

            //act
            var capability = new TargetSystemCapability(summary);

            //assert
            Assert.True(capability.HasUrl);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("random text")]
        public void ShouldNotHaveUrl(string url)
        {
            //arrange
            var summary = CreateSampleSummary(url);

            //act
            var capability = new TargetSystemCapability(summary);

            //assert
            Assert.False(capability.HasUrl);
        }
    }
}
