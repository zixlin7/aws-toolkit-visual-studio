using Amazon.AWSToolkit.Settings;

namespace AwsToolkit.VsSdk.Common.Settings.Proxy
{
    public interface IProxySettingsRepository
    {
        /// <summary>
        /// Load the current Proxy settings <see cref="ProxySettings"/>
        /// </summary>
        /// <returns></returns>
       ProxySettings Get();

        /// <summary>
        /// Update the current proxy settings with the given values <see cref="ProxySettings"/>
        /// </summary>
        void Save(ProxySettings settings);
    }

    public class ProxySettingsRepository : IProxySettingsRepository
    {
        public ProxySettings Get()
        {
            return ProxyUtilities.RetrieveCurrentSettings();
        }

        public void Save(ProxySettings settings)
        {
            ProxyUtilities.ApplyProxySettings(settings);
        }
    }
}
