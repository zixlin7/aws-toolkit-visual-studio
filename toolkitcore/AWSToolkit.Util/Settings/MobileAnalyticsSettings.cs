namespace Amazon.AWSToolkit.Settings
{
    public class MobileAnalyticsSettings
    {
        private readonly SettingsPersistenceBase _settingsPersistence;

        public MobileAnalyticsSettings(SettingsPersistenceBase settingsPersistence)
        {
            _settingsPersistence = settingsPersistence;
        }

        public string LastUsedCognitoIdentityPoolId
        {
            get =>
                _settingsPersistence.GetString(ToolkitSettingsConstants
                    .AnalyticsMostRecentlyUsedCognitoIdentityPoolId);
            set
            {
                if (LastUsedCognitoIdentityPoolId != value)
                {
                    _settingsPersistence.SetString(
                        ToolkitSettingsConstants.AnalyticsMostRecentlyUsedCognitoIdentityPoolId, value);
                }
            }
        }

        public string CognitoIdentityId
        {
            get =>
                _settingsPersistence.GetString(ToolkitSettingsConstants
                    .AnalyticsCognitoIdentityId);
            set
            {
                if (CognitoIdentityId != value)
                {
                    _settingsPersistence.SetString(
                        ToolkitSettingsConstants.AnalyticsCognitoIdentityId, value);
                }
            }
        }
    }
}