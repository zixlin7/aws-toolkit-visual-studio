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

        private readonly SettingsPersistenceBase _settingsPersistence;
        private readonly DynamoDbSettings _dynamoDbSettings;

        private static class SettingNames
        {
            public const string TelemetryEnabled = "AnalyticsPermitted";
            public const string TelemetryNoticeVersionShown = "TelemetryNoticeVersionShown";
            public const string FirstRunFormShown = "FirstRunFormShown";
            public const string TelemetryClientId = "AnalyticsAnonymousCustomerId";
            public const string LastSelectedRegion = "lastselectedregion";
            public const string ShowMetricsOutputWindow = "ShowMetricsOutputWindow";
            public const string Vs2017SunsetNoticeVersionShown = "Vs2017SunsetNoticeVersionShown";
            public const string UseLegacyAccountUx = "UseLegacyAccountUx";
        }

        public static class DefaultValues
        {
            public const bool TelemetryEnabled = true; // Opt-out model
            public const bool HasUserSeenFirstRunForm = false;
            public const bool ShowMetricsOutputWindow = false;
            public const int Vs2017SunsetNoticeVersionNeverShown = 0;
            public const bool UseLegacyAccountUx = true;
        }

        static ToolkitSettings()
        {
            Initialize();
        }

        public static void Initialize()
        {
            Initialize(new SettingsPersistence());
        }

        public static void Initialize(SettingsPersistenceBase settingsPersistence)
        {
            Instance = new ToolkitSettings(settingsPersistence);
        }

        /// <summary>
        /// Constructor is protected so that we can test this class
        /// without using the singleton instance.
        /// </summary>
        protected ToolkitSettings(SettingsPersistenceBase settingsPersistence)
        {
            _settingsPersistence = settingsPersistence;
            _dynamoDbSettings = new DynamoDbSettings(_settingsPersistence);
        }

        public DynamoDbSettings DynamoDb
        {
            get { return _dynamoDbSettings; }
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
            get =>
                _settingsPersistence.GetInt(SettingNames.TelemetryNoticeVersionShown,
                    TelemetryNoticeNeverShown);
            set => _settingsPersistence.SetInt(SettingNames.TelemetryNoticeVersionShown, value);
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
        /// Gets or sets whether or not to use the legacy account UX
        /// </summary>
        public bool UseLegacyAccountUx
        {
            get
            {
                var valueStr = GetMiscSetting(SettingNames.UseLegacyAccountUx);
                if (!bool.TryParse(valueStr, out var useLegacy))
                {
                    useLegacy = DefaultValues.UseLegacyAccountUx;
                }

                return useLegacy;
            }

            set => SetMiscSetting(SettingNames.UseLegacyAccountUx, AsString(value));
        }

        public string LastSelectedCredentialId
        {
            get => GetMiscSetting(ToolkitSettingsConstants.LastSelectedCredentialId);
            set => SetMiscSetting(ToolkitSettingsConstants.LastSelectedCredentialId, value);
        }

        public string LastSelectedRegion
        {
            get => GetMiscSetting(SettingNames.LastSelectedRegion);
            set => SetMiscSetting(SettingNames.LastSelectedRegion, value);
        }

        public string HostedFilesLocation
        {
            get => GetMiscSetting(ToolkitSettingsConstants.HostedFilesLocation);
            set => SetMiscSetting(ToolkitSettingsConstants.HostedFilesLocation, value);
        }

        /// <summary>
        /// Used by Toolkit developers to easily see the generated metrics in a VS Output Window Pane
        /// </summary>
        public bool ShowMetricsOutputWindow
        {
            get
            {
                var valueStr = GetMiscSetting(SettingNames.ShowMetricsOutputWindow);
                if (!bool.TryParse(valueStr, out var showWindow))
                {
                    showWindow = DefaultValues.ShowMetricsOutputWindow;
                }

                return showWindow;
            }
            set => SetMiscSetting(SettingNames.ShowMetricsOutputWindow, AsString(value));
        }


        /// <summary>
        /// Gets or sets the "version" of the VS2017 deprecation notice/InfoBar the user last saw and acknowledged.
        /// </summary>
        public int Vs2017SunsetNoticeVersionShown
        {
            get =>
                _settingsPersistence.GetInt(SettingNames.Vs2017SunsetNoticeVersionShown,
                    DefaultValues.Vs2017SunsetNoticeVersionNeverShown);
            set => _settingsPersistence.SetInt(SettingNames.Vs2017SunsetNoticeVersionShown, value);
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
            var value = _settingsPersistence.GetString(name);
            return value ?? defaultValue;
        }

        private void SetMiscSetting(string name, string value)
        {
            _settingsPersistence.SetString(name, value);
        }

        private string AsString(bool value)
        {
            return value.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture);
        }
    }
}
