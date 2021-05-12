using System;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Tests.Common.Settings;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Settings
{
    public class ToolkitSettingsTests
    {
        private static class PersistenceFields
        {
            public const string LastSelectedCredentialId = ToolkitSettingsConstants.LastSelectedCredentialId;
            public const string LastSelectedRegion = "lastselectedregion";
            public const string HostedFilesLocation = ToolkitSettingsConstants.HostedFilesLocation;
        }

        /// <summary>
        /// Thin wrapper around the ToolkitSettings class being tested so that
        /// tests do not use the singleton.
        /// </summary>
        public class TestToolkitSettings : ToolkitSettings
        {
            public TestToolkitSettings(SettingsPersistenceBase settingsPersistence)
                : base(settingsPersistence)
            {
            }
        }

        private readonly FakeSettingsPersistence _settingsPersistence = new FakeSettingsPersistence();
        private readonly ToolkitSettings _sut;

        public ToolkitSettingsTests()
        {
            _sut = new TestToolkitSettings(_settingsPersistence);
        }

        [Theory]
        [InlineData(null, ToolkitSettings.DefaultValues.TelemetryEnabled)]
        [InlineData("garbage", ToolkitSettings.DefaultValues.TelemetryEnabled)]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void GetTelemetryEnabled(string persistedValue, bool expectedValue)
        {
            if (persistedValue != null)
            {
                _settingsPersistence.PersistenceData["AnalyticsPermitted"] = persistedValue;
            }

            Assert.Equal(expectedValue, _sut.TelemetryEnabled);
        }

        [Theory]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        public void SetTelemetryEnabled(bool value, string persistedValue)
        {
            _sut.TelemetryEnabled = value;

            Assert.Equal(persistedValue, _settingsPersistence.PersistenceData["AnalyticsPermitted"]);
        }

        [Theory]
        [InlineData(null, 0)]
        [InlineData("garbage", 0)]
        [InlineData("0", 0)]
        [InlineData("1", 1)]
        [InlineData("2", 2)]
        public void GetTelemetryNoticeVersionShown(string persistedValue, int expectedValue)
        {
            if (persistedValue != null)
            {
                _settingsPersistence.PersistenceData["TelemetryNoticeVersionShown"] = persistedValue;
            }

            Assert.Equal(expectedValue, _sut.TelemetryNoticeVersionShown);
        }

        [Theory]
        [InlineData(0, "0")]
        [InlineData(1, "1")]
        public void SetTelemetryNoticeVersionShown(int value, string persistedValue)
        {
            _sut.TelemetryNoticeVersionShown = value;

            Assert.Equal(persistedValue, _settingsPersistence.PersistenceData["TelemetryNoticeVersionShown"]);
        }

        [Theory]
        [InlineData(null, ToolkitSettings.DefaultValues.HasUserSeenFirstRunForm)]
        [InlineData("garbage", ToolkitSettings.DefaultValues.HasUserSeenFirstRunForm)]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void GetHasUserSeenFirstRunForm(string persistedValue, bool expectedValue)
        {
            if (persistedValue != null)
            {
                _settingsPersistence.PersistenceData["FirstRunFormShown"] = persistedValue;
            }

            Assert.Equal(expectedValue, _sut.HasUserSeenFirstRunForm);
        }

        [Theory]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        public void SetHasUserSeenFirstRunForm(bool value, string persistedValue)
        {
            _sut.HasUserSeenFirstRunForm = value;

            Assert.Equal(persistedValue, _settingsPersistence.PersistenceData["FirstRunFormShown"]);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("garbage", true)]
        [InlineData("316074D1-47A1-4154-A79C-B98723154B4E", false)]
        public void GetTelemetryClientId(string persistedValue, bool isNull)
        {
            if (persistedValue != null)
            {
                _settingsPersistence.PersistenceData["AnalyticsAnonymousCustomerId"] = persistedValue;
            }

            Assert.Equal(!isNull, _sut.TelemetryClientId.HasValue);
        }

        [Fact]
        public void SetTelemetryClientId()
        {
            var guid = Guid.NewGuid();
            _sut.TelemetryClientId = guid;

            Assert.Equal(guid.ToString(), _settingsPersistence.PersistenceData["AnalyticsAnonymousCustomerId"]);
        }

        [Fact]
        public void GetLastSelectedCredentialId()
        {
            _settingsPersistence.PersistenceData[PersistenceFields.LastSelectedCredentialId] = "hello";
            Assert.Equal("hello", _sut.LastSelectedCredentialId);
        }

        [Fact]
        public void GetLastSelectedCredentialIdReturnsDefault()
        {
            Assert.Null(_sut.LastSelectedCredentialId);
        }

        [Fact]
        public void SetLastSelectedCredentialId()
        {
            _sut.LastSelectedCredentialId = "hi";
            Assert.Equal("hi",
                _settingsPersistence.PersistenceData[
                    PersistenceFields.LastSelectedCredentialId]);
        }

        [Fact]
        public void GetLastSelectedRegion()
        {
            _settingsPersistence.PersistenceData[PersistenceFields.LastSelectedRegion] = "hello";
            Assert.Equal("hello", _sut.LastSelectedRegion);
        }

        [Fact]
        public void GetLastSelectedRegionReturnsDefault()
        {
            Assert.Null(_sut.LastSelectedRegion);
        }

        [Fact]
        public void SetLastSelectedRegion()
        {
            _sut.LastSelectedRegion = "hi";
            Assert.Equal("hi",
                _settingsPersistence.PersistenceData[PersistenceFields.LastSelectedRegion]);
        }

        [Fact]
        public void GetHostedFilesLocation()
        {
            _settingsPersistence.PersistenceData[PersistenceFields.HostedFilesLocation] = "hello";
            Assert.Equal("hello", _sut.HostedFilesLocation);
        }

        [Fact]
        public void GetHostedFilesLocationReturnsDefault()
        {
            Assert.Null(_sut.HostedFilesLocation);
        }

        [Fact]
        public void SetHostedFilesLocation()
        {
            _sut.HostedFilesLocation = "hi";
            Assert.Equal("hi",
                _settingsPersistence.PersistenceData[
                    PersistenceFields.HostedFilesLocation]);
        }
    }
}
