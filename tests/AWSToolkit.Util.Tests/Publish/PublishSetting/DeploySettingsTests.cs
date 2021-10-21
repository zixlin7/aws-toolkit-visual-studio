using Amazon.AWSToolkit.Publish.PublishSetting;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Publish.PublishSetting
{
    public class DeploySettingsTests
    {
        [Fact]
        public void ShouldCreate()
        {
            // arrange.
            var portRangeStart = 20000;
            var portRangeEnd = 20001;

            // act.
            var portRange = new PortRange(portRangeStart, portRangeEnd);
            var settings = new DeployServerSettings(portRange);

            // assert.
            Assert.Equal(portRangeStart, settings.PortRange.Start);
            Assert.Equal(portRangeEnd, settings.PortRange.End);
        }

        [Fact]
        public void ShouldCreateDefault()
        {
            var expectedDefaultSettings = new DeployServerSettings(new PortRange(10000, 10100));
            Assert.Equal(expectedDefaultSettings, DeployServerSettings.CreateDefault());
        }
    }
}
