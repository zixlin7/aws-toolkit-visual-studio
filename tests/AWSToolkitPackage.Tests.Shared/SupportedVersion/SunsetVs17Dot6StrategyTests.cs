using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Tests.Common.Settings;
using Amazon.AWSToolkit.VisualStudio.SupportedVersion;

using FluentAssertions;

using Xunit;

namespace AWSToolkitPackage.Tests.SupportedVersion
{
    public class SunsetVs17Dot6StrategyTests
    {
        private readonly FakeSettingsRepository<SunsetNotificationSettings> _settingsRepository = new FakeSettingsRepository<SunsetNotificationSettings>();

#if VS2022
        public static TheoryData<Version, bool> GetCanShowInputs()
        {
            return new TheoryData<Version, bool>()
            {
                { new Version(17, 0), true },
                { new Version(17, 6), true },
                { new Version(17, 7), false},
                { new Version(17, 8), false},
            };
        }

        [Theory]
        [MemberData(nameof(GetCanShowInputs))]
        public async Task CanShowNoticeAsync_Vs2022(Version vsVersion, bool expectedResult)
        {
            var sut = CreateSut(vsVersion);

            var canShowNotice = await sut.CanShowNoticeAsync();
            canShowNotice.Should().Be(expectedResult);
        }
#endif

#if VS2019
        [Fact]
        public async Task CanShowNoticeAsync_Vs2019()
        {
            var sut = CreateSut(new Version(16, 0));

            var canShowNotice = await sut.CanShowNoticeAsync();
            canShowNotice.Should().BeFalse();
        }
#endif

        private SunsetVs17Dot6Strategy CreateSut(Version vsVersion)
        {
            return new SunsetVs17Dot6Strategy(vsVersion, _settingsRepository);
        }
    }
}
