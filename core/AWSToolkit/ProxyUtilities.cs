using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit
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

                settings.Username = proxySettings[SettingsConstants.ProxyUsername];
                settings.Password = proxySettings[SettingsConstants.ProxyPassword];
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
            
            objectSettings[SettingsConstants.ProxyUsername] = settings.Username;
            objectSettings[SettingsConstants.ProxyPassword] = settings.Password;

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


        /// <summary>
        /// Settings for configuring a proxy for the SDK to use.
        /// </summary>
        public class ProxySettings
        {
            /// <summary>
            /// Gets and sets the host name or IP address of the proxy server.
            /// </summary>
            public string Host
            {
                get;
                set;
            }

            /// <summary>
            /// Gets and sets the port number of the proxy.
            /// </summary>
            public int? Port
            {
                get;
                set;
            }

            /// <summary>
            /// Gets and sets the username to authenticate with the proxy server.
            /// </summary>
            public string Username
            {
                get;
                set;
            }

            /// <summary>
            /// Gets and sets the password to authenticate with the proxy server.
            /// </summary>
            public string Password
            {
                get;
                set;
            }
        }
    }
}
