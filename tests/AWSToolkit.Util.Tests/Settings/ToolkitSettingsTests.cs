using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Settings;
using Amazon.Runtime.Internal.Settings;
using Moq;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Settings
{
    public class ToolkitSettingsTests
    {
        private readonly Mock<SettingsPersistence> _persistenceManager = new Mock<SettingsPersistence>();
        private readonly Dictionary<string, string> _persistenceData = new Dictionary<string, string>();

        public ToolkitSettingsTests()
        {
            _persistenceManager.Setup(mock => mock.GetSetting(It.IsAny<string>()))
                .Returns<string>(name =>
                {
                    if (_persistenceData.TryGetValue(name, out string value))
                    {
                        return value;
                    }

                    return null;
                });

            _persistenceManager.Setup(mock => mock.SetSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((name, value)=>
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        _persistenceData.Remove(name);
                    }
                    else
                    {
                        _persistenceData[name] = value;
                    }
                });

            ToolkitSettings.Initialize(_persistenceManager.Object);
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
                _persistenceData["AnalyticsPermitted"] = persistedValue;
            }

            Assert.Equal(expectedValue, ToolkitSettings.Instance.TelemetryEnabled);
        }

        [Theory]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        public void SetTelemetryEnabled(bool value, string persistedValue)
        {
            ToolkitSettings.Instance.TelemetryEnabled = value;

            Assert.Equal(persistedValue, _persistenceData["AnalyticsPermitted"]);
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
                _persistenceData["TelemetryNoticeVersionShown"] = persistedValue;
            }

            Assert.Equal(expectedValue, ToolkitSettings.Instance.TelemetryNoticeVersionShown);
        }

        [Theory]
        [InlineData(0, "0")]
        [InlineData(1, "1")]
        public void SetTelemetryNoticeVersionShown(int value, string persistedValue)
        {
            ToolkitSettings.Instance.TelemetryNoticeVersionShown = value;

            Assert.Equal(persistedValue, _persistenceData["TelemetryNoticeVersionShown"]);
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
                _persistenceData["FirstRunFormShown"] = persistedValue;
            }

            Assert.Equal(expectedValue, ToolkitSettings.Instance.HasUserSeenFirstRunForm);
        }

        [Theory]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        public void SetHasUserSeenFirstRunForm(bool value, string persistedValue)
        {
            ToolkitSettings.Instance.HasUserSeenFirstRunForm = value;

            Assert.Equal(persistedValue, _persistenceData["FirstRunFormShown"]);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("garbage", true)]
        [InlineData("316074D1-47A1-4154-A79C-B98723154B4E", false)]
        public void GetTelemetryClientId(string persistedValue, bool isNull)
        {
            if (persistedValue != null)
            {
                _persistenceData["AnalyticsAnonymousCustomerId"] = persistedValue;
            }

            Assert.Equal(!isNull, ToolkitSettings.Instance.TelemetryClientId.HasValue);
        }

        [Fact]
        public void SetTelemetryClientId()
        {
            var guid = Guid.NewGuid();
            ToolkitSettings.Instance.TelemetryClientId = guid;

            Assert.Equal(guid.ToString(), _persistenceData["AnalyticsAnonymousCustomerId"]);
        }
    }
}