using System.Threading.Tasks;
using Amazon.AWSToolkit.VisualStudio.Notification;
using FluentAssertions;
using Xunit;

namespace AWSToolkitPackage.Tests.Integration.Notification
{
    public class VersionIntegrationTest
    {
        private readonly VersionStrategy _sut;
        private const string _sampleProductVersion = "1.39.0.0";

        public VersionIntegrationTest()
        {
            _sut = new VersionStrategy(_sampleProductVersion);
        }

        [Theory]
        [MemberData(nameof(GetVersionData))]
        public async Task ShouldCompareProductAndLatestVersionsAsync(DisplayIf latestVersion, bool expected)
        {
            var notification = new Amazon.AWSToolkit.VisualStudio.Notification.Notification() { DisplayIf = latestVersion };

            var result = await _sut.IsVersionWithinDisplayConditionsAsync(notification);

            result.Should().Be(expected);
        }

        public static TheoryData<DisplayIf, bool> GetVersionData()
        {
            return new TheoryData<DisplayIf, bool>()
            {
                {new DisplayIf() {ToolkitVersion = "latest", Comparison = "<"}, true},
                {new DisplayIf() {ToolkitVersion = "current", Comparison = "<"}, true},
                {new DisplayIf() {ToolkitVersion = "latest", Comparison = ">="}, false},
                {new DisplayIf() {ToolkitVersion = "current", Comparison = ">="}, false}
            };
        }
    }
}
