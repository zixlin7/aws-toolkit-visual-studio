using Amazon.AWSToolkit.VisualStudio.Notification;
using System;
using System.Threading.Tasks;

using Xunit;

namespace AWSToolkitPackage.Tests.Notification
{
    public class VersionStrategyTests
    {
        private readonly VersionStrategy _sut;
        private const string _sampleProductVersion = "1.39.0.0";

        public VersionStrategyTests()
        {
            _sut = new VersionStrategy(_sampleProductVersion, () => Task.FromResult(new Version(_sampleProductVersion)));
        }

        [Theory]
        [MemberData(nameof(GetVersionData))]
        public async Task ShouldCompareProductAndDisplayVersionsAsync(DisplayIf displayIf, bool result)
        {
            var notification = new Amazon.AWSToolkit.VisualStudio.Notification.Notification() { DisplayIf = displayIf };
            Assert.Equal(await _sut.IsVersionWithinDisplayConditionsAsync(notification), result);
        }

        public static TheoryData<DisplayIf, bool> GetVersionData()
        {
            return new TheoryData<DisplayIf, bool>()
            {
                {new DisplayIf() {ToolkitVersion = "1.40.0.0", Comparison = "<"}, true},
                {new DisplayIf() {ToolkitVersion = "1.100.0.0", Comparison = "<"}, true },
                {new DisplayIf() {ToolkitVersion = "1.39.0.0", Comparison = "<"}, false},
                {new DisplayIf() {ToolkitVersion = "1.39.0.0", Comparison = "<="}, true},
                {new DisplayIf() {ToolkitVersion = "1.39.0.0", Comparison = ">="}, true},
                {new DisplayIf() {ToolkitVersion = "1.39.0.0", Comparison = ">"}, false},
                {new DisplayIf() {ToolkitVersion = "1.38.2.0", Comparison = ">"}, true},
                {new DisplayIf() {ToolkitVersion = "1.39.0.0", Comparison = "=="}, true},
                {new DisplayIf() {ToolkitVersion = "1.39.0.0", Comparison = "!="}, false},
                {new DisplayIf() {ToolkitVersion = "1.39.1.0", Comparison = "<="}, true},
                {new DisplayIf() {ToolkitVersion = "latest", Comparison = "=="}, true },
                {new DisplayIf() {ToolkitVersion = "current", Comparison = "=="} , true }
            };
        }
    }
}
