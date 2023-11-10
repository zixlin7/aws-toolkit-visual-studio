using Amazon.AWSToolkit.VisualStudio.SupportedVersion;

using FluentAssertions;

using Xunit;

namespace AWSToolkitPackage.Tests.SupportedVersion
{
    public class SunsetNotificationSettingsTests
    {
        private const string _sampleId = "sample-id";
        private readonly SunsetNotificationSettings _sut = new SunsetNotificationSettings();

        [Fact]
        public void GetDisplayedNotificationVersion_WhenNotSet()
        {
            _sut.GetDisplayedNotificationVersion(_sampleId, -1).Should().Be(-1);
        }

        [Fact]
        public void GetDisplayedNotificationVersion_WhenSet()
        {
            _sut.SetDisplayedNotificationVersion(_sampleId, 2);
            _sut.GetDisplayedNotificationVersion(_sampleId, -1).Should().Be(2);
        }

        [Fact]
        public void SetDisplayedNotificationVersion()
        {
            _sut.SetDisplayedNotificationVersion(_sampleId, 3);
            _sut.DisplayedNotifications[_sampleId].Should().Be(3);
        }
    }
}
