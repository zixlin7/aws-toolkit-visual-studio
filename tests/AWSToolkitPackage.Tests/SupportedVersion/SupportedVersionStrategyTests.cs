using System.Collections.Generic;

using Amazon.AWSToolkit.Tests.Common.Settings;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.VisualStudio.SupportedVersion;

using Xunit;

namespace AWSToolkitPackage.Tests.SupportedVersion
{
    public class SupportedVersionStrategyTests
    {
        private readonly FakeToolkitSettings _fakeToolkitSettings = FakeToolkitSettings.Create();
        private readonly SupportedVersionStrategy _sut;

        public static readonly IEnumerable<object[]> SupportedVersionData = new List<object[]>
        {
            new object[] {ToolkitHosts.Vs2017, false},
            new object[] {ToolkitHosts.Vs2019, true},
            new object[] {ToolkitHosts.Vs2022, true}
        };

        public static readonly IEnumerable<object[]> HasUserSeenNoticeData = new List<object[]>
        {
            new object[] {0, false},
            new object[] {1, true},
            new object[] {2, true}
        };


        public static readonly IEnumerable<object[]> CanShowNoticeData = new List<object[]>
        {
            new object[] {ToolkitHosts.Vs2017, 0, true},
            new object[] {ToolkitHosts.Vs2019, 0, false},
            new object[] {ToolkitHosts.Vs2022, 0, false},
            new object[] {ToolkitHosts.Vs2017, 1, false},
            new object[] {ToolkitHosts.Vs2017, 2, false},
        };

        public static readonly IEnumerable<object[]> EmptyMessageData = new List<object[]>
        {
            new object[] {ToolkitHosts.Vs2019},
            new object[] {ToolkitHosts.Vs2022}
        };

        public SupportedVersionStrategyTests()
        {
            _sut = new SupportedVersionStrategy(ToolkitHosts.Vs2017, ToolkitHosts.Vs2017, _fakeToolkitSettings);
        } 

        [Theory]
        [MemberData(nameof(SupportedVersionData))]
        public void IsSupportedVersion(IToolkitHostInfo hostInfo, bool expectedResult)
        {
            _sut.CurrentHost = hostInfo;
            Assert.Equal(expectedResult, _sut.IsSupportedVersion());
        }

        [Theory]
        [MemberData(nameof(HasUserSeenNoticeData))]
        public void HasUserSeenNotice_When2017(int currentNoticeVersion, bool expectedResult)
        {
            _fakeToolkitSettings.Vs2017SunsetNoticeVersionShown = currentNoticeVersion;
            Assert.Equal(expectedResult, _sut.HasUserSeenNotice());
        }

        [Fact]
        public void MarkNoticeAsShown_When2017()
        {
            Assert.False(_sut.HasUserSeenNotice());

            _sut.MarkNoticeAsShown();

            Assert.True(_sut.HasUserSeenNotice());
        }

        [Fact]
        public void GetMessage_When2017()
        {
            Assert.Contains("Visual Studio 2017", _sut.GetMessage());
        }

        [Theory]
        [MemberData(nameof(EmptyMessageData))]
        public void GetMessage_WhenNot2017(IToolkitHostInfo hostDeprecated)
        {
            _sut.HostDeprecated = hostDeprecated;
            Assert.Empty(_sut.GetMessage());
        }

        [Theory]
        [MemberData(nameof(CanShowNoticeData))]
        public void CanShowNotice(IToolkitHostInfo currentHost, int currentNoticeVersion, bool expectedResult)
        {
            _sut.CurrentHost = currentHost;
            _fakeToolkitSettings.Vs2017SunsetNoticeVersionShown = currentNoticeVersion;
            Assert.Equal(expectedResult, _sut.CanShowNotice());
        }
    }
}
