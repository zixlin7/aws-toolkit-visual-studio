namespace Amazon.AWSToolkit.Settings
{
    /// <summary>
    /// Settings for configuring a proxy for the SDK to use.
    /// </summary>
    public class ProxySettings
    {
        /// <summary>
        ///  Host name or IP address of the proxy server.
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// Port number of the proxy.
        /// </summary>
        public int? Port { get; set; }
        /// <summary>
        /// Username to authenticate with the proxy server.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Password to authenticate with the proxy server.
        /// </summary>
        public string Password { get; set; }
    }
}
