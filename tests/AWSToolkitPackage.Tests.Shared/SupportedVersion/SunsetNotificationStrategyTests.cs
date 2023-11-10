using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Tests.Common.Settings;
using Amazon.AWSToolkit.VisualStudio.SupportedVersion;

using FluentAssertions;

using Xunit;

namespace AWSToolkitPackage.Tests.SupportedVersion
{
    public class StubSunsetNotificationStrategy : SunsetNotificationStrategy
    {
        public override string Identifier { get; } = "stubId";

        protected override Version _futureMinimumRequiredVersion { get; }

        public StubSunsetNotificationStrategy(
            Version vsVersion,
            Version minimumRequiredVersion,
            ISettingsRepository<SunsetNotificationSettings> settingsRepository)
            : base(vsVersion, settingsRepository)
        {
            _futureMinimumRequiredVersion = minimumRequiredVersion;
        }

        public override string GetMessage()
        {
            throw new NotImplementedException(nameof(GetMessage));
        }
    }

    public class SunsetNotificationStrategyTests
    {
        private readonly FakeSettingsRepository<SunsetNotificationSettings> _settingsRepository = new FakeSettingsRepository<SunsetNotificationSettings>();

        public static TheoryData<Version, Version, bool> GetCanShowInputs()
        {
            return new TheoryData<Version, Version, bool>()
            {
                { new Version(16, 0), new Version(16, 0), false },
                { new Version(16, 0), new Version(15, 0), false },
                { new Version(16, 0), new Version(17, 0), true },
                { new Version(16, 0), new Version(16, 5), true },
            };
        }

        [Theory]
        [MemberData(nameof(GetCanShowInputs))]
        public async Task CanShowNoticeAsync(Version vsVersion, Version minimumRequiredVersion, bool expectedResult)
        {
            var sut = CreateSut(vsVersion, minimumRequiredVersion);

            var canShow = await sut.CanShowNoticeAsync();
            canShow.Should().Be(expectedResult);
        }

        [Fact]
        public async Task CanShowNoticeAsync_WhenDismissed()
        {
            var sut = CreateSut(new Version(17, 0), new Version(17, 7));
            await sut.MarkAsSeenAsync();

            var canShow = await sut.CanShowNoticeAsync();
            canShow.Should().BeFalse();
        }

        [Fact]
        public async Task MarkAsSeenAsync()
        {
            var sut = CreateSut(new Version(17, 0), new Version(17, 7));

            await sut.MarkAsSeenAsync();
            _settingsRepository.Settings.GetDisplayedNotificationVersion(sut.Identifier, -1).Should().Be(1);
        }

        private StubSunsetNotificationStrategy CreateSut(Version vsVersion, Version minimumRequiredVersion)
        {
            return new StubSunsetNotificationStrategy(vsVersion, minimumRequiredVersion, _settingsRepository);
        }
    }
}
