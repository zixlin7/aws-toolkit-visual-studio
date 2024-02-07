using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit.Settings
{
    public class ProxyUtilities
    {
        public static ProxySettings RetrieveCurrentSettings()
        {
            var settings = new ProxySettings();

            var userSettings = PersistenceManager.Instance.GetSettings(SettingsConstants.UserPreferences);
            var proxySettings = userSettings[SettingsConstants.ProxySettings];

            if (proxySettings != null)
            {
                settings.Host = proxySettings[SettingsConstants.ProxyHost];

                int port;
                if (int.TryParse(proxySettings[SettingsConstants.ProxyPort], out port))
                {
                    settings.Port = port;
                }

                // Migrate old insecure setting to new encrypted settings.
                if (proxySettings[SettingsConstants.ProxyUsernameObsolete] != null || proxySettings[SettingsConstants.ProxyPasswordObsolete] != null)
                {
                    proxySettings[SettingsConstants.ProxyUsernameEncrypted] = proxySettings[SettingsConstants.ProxyUsernameObsolete];
                    proxySettings[SettingsConstants.ProxyPasswordEncrypted] = proxySettings[SettingsConstants.ProxyPasswordObsolete];

                    proxySettings[SettingsConstants.ProxyUsernameObsolete] = null;
                    proxySettings[SettingsConstants.ProxyPasswordObsolete] = null;

                    PersistenceManager.Instance.SaveSettings(SettingsConstants.UserPreferences, userSettings);
                }

                settings.Username = proxySettings[SettingsConstants.ProxyUsernameEncrypted];
                settings.Password = proxySettings[SettingsConstants.ProxyPasswordEncrypted];
            }


            return settings;
        }

        public static void ApplyProxySettings(ProxySettings settings)
        {
            var userSettings = PersistenceManager.Instance.GetSettings(SettingsConstants.UserPreferences);
            var objectSettings = userSettings[SettingsConstants.ProxySettings];

            objectSettings[SettingsConstants.ProxyHost] = settings.Host;
            if (settings.Port.HasValue)
                objectSettings[SettingsConstants.ProxyPort] = settings.Port.ToString();
            else
                objectSettings.Remove(SettingsConstants.ProxyPort);
            
            objectSettings[SettingsConstants.ProxyUsernameEncrypted] = settings.Username;
            objectSettings[SettingsConstants.ProxyPasswordEncrypted] = settings.Password;

            PersistenceManager.Instance.SaveSettings(SettingsConstants.UserPreferences, userSettings);

            ApplySettingsToSDK(settings);
        }


        public static void ApplyCurrentProxySettings()
        {
            var settings = RetrieveCurrentSettings();
            ApplySettingsToSDK(settings);
        }

        private static void ApplySettingsToSDK(ProxySettings settings)
        {
            var proxy = AWSConfigs.ProxyConfig;

            proxy.Host = settings.Host;
            proxy.Port = settings.Port;
            proxy.Username = settings.Username;
            proxy.Password = settings.Password;
        }
    }
}
