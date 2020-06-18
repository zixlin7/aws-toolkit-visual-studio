using System;
using System.Globalization;

namespace Amazon.AWSToolkit.Settings
{
    /// <summary>
    /// Centralized Get/Set logic for Toolkit Settings
    /// TODO : over time, converge much of the toolkit code that deals with settings into here
    /// </summary>
    public class ToolkitSettings
    {
        public static ToolkitSettings Instance;

        private const int TelemetryNoticeNeverShown = 0;

        private readonly SettingsPersistence _settingsPersistence;

        private static class SettingNames
        {
            public const string TelemetryEnabled = "AnalyticsPermitted";
            public const string TelemetryNoticeVersionShown = "TelemetryNoticeVersionShown";
            public const string FirstRunFormShown = "FirstRunFormShown";
            public const string TelemetryClientId = "AnalyticsAnonymousCustomerId";
        }

        public static class DefaultValues
        {
            public const bool TelemetryEnabled = true; // Opt-out model
            public const bool HasUserSeenFirstRunForm = false;
        }

        public static void Initialize()
        {
            Initialize(new SettingsPersistence());
        }

        public static void Initialize(SettingsPersistence settingsPersistence)
        {
            Instance = new ToolkitSettings(settingsPersistence);
        }

        private ToolkitSettings(SettingsPersistence settingsPersistence)
        {
            _settingsPersistence = settingsPersistence;
        }

        /// <summary>
        /// Gets or sets whether or not Telemetry is enabled
        /// </summary>
        public bool TelemetryEnabled
        {
            get
            {
                var valueStr = GetMiscSetting(SettingNames.TelemetryEnabled);
                if (!bool.TryParse(valueStr, out var enabled))
                {
                    enabled = DefaultValues.TelemetryEnabled;
                }

                return enabled;
            }

            set => SetMiscSetting(SettingNames.TelemetryEnabled, AsString(value));
        }

        /// <summary>
        /// Gets or sets the "version" of the telemetry notice/InfoBar the user last saw and acknowledged.
        /// </summary>
        public int TelemetryNoticeVersionShown
        {
            get
            {
                var versionStr = GetMiscSetting(SettingNames.TelemetryNoticeVersionShown);
                if (!int.TryParse(versionStr, NumberStyles.None, CultureInfo.InvariantCulture, out var version))
                {
                    return TelemetryNoticeNeverShown;
                }

                return version;
            }

            set
            {
                var version = value.ToString(CultureInfo.InvariantCulture);
                SetMiscSetting(SettingNames.TelemetryNoticeVersionShown, version);
            }
        }

        /// <summary>
        /// Gets or sets whether or not the user has seen the First Run form
        /// </summary>
        public bool HasUserSeenFirstRunForm
        {
            get
            {
                var valueStr = GetMiscSetting(SettingNames.FirstRunFormShown);
                if (!bool.TryParse(valueStr, out var hasSeen))
                {
                    hasSeen = DefaultValues.HasUserSeenFirstRunForm;
                }

                return hasSeen;
            }

            set => SetMiscSetting(SettingNames.FirstRunFormShown, AsString(value));
        }

        /// <summary>
        /// Gets or sets a guid that represents this toolkit installation.
        /// Null represents that the value is unset or cannot be parsed.
        /// </summary>
        public Guid? TelemetryClientId
        {
            get
            {
                var clientIdStr = GetMiscSetting(SettingNames.TelemetryClientId);

                if (string.IsNullOrEmpty(clientIdStr) || !Guid.TryParse(clientIdStr, out var clientId))
                {
                    return null;
                }

                return clientId;
            }

            set => SetMiscSetting(SettingNames.TelemetryClientId, value.ToString());
        }

        private string GetMiscSetting(string name, string defaultValue = null)
        {
            var value = _settingsPersistence.GetSetting(name);
            return value ?? defaultValue;
        }

        private void SetMiscSetting(string name, string value)
        {
            _settingsPersistence.SetSetting(name, value);
        }

        private string AsString(bool value)
        {
            return value.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture);
        }
    }
}