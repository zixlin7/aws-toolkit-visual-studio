using System;

namespace Amazon.AWSToolkit.Settings
{
    public static class ProxySettingsExtensionMethods
    {
        /// <summary>
        /// Get Proxy url constructed by referencing https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-proxy.html#cli-configure-proxy-auth
        /// </summary>
        public static string GetProxyUrl(this ProxySettings proxySettings)
        {
            if (proxySettings == null)
            {
                return null;
            }
            var proxyHost = GetHostName(proxySettings.Host);

            // if either host or port is missing, return null
            if (string.IsNullOrWhiteSpace(proxyHost) || proxySettings.Port == null)
            {
                return null;
            }
            var proxyProtocol = GetProxyProtocol(proxySettings.Host);
            var proxyUrl = $"{proxyProtocol}://{proxyHost}:{proxySettings.Port}";

            // add authentication fields to the url if it has been setup
            if (!string.IsNullOrWhiteSpace(proxySettings.Username) &&
                !string.IsNullOrWhiteSpace(proxySettings.Password))
            {
                proxyUrl =
                    $"{proxyProtocol}://{proxySettings.Username}:{proxySettings.Password}@{proxyHost}:{proxySettings.Port}";
            }

            return proxyUrl;
        }


        private static string GetProxyProtocol(string proxySettingsHost)
        {
            return Uri.IsWellFormedUriString(proxySettingsHost, UriKind.Absolute)
                ? new Uri(proxySettingsHost).Scheme
                : "http";
        }

        private static string GetHostName(string proxySettingsHost)
        {
            if (string.IsNullOrWhiteSpace(proxySettingsHost))
            {
                return proxySettingsHost;
            }

            return Uri.IsWellFormedUriString(proxySettingsHost, UriKind.Absolute)
                ? new Uri(proxySettingsHost).Host
                : proxySettingsHost;
        }
    }
}
