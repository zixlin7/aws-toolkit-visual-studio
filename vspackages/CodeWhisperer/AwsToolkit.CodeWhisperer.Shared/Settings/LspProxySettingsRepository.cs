using System.Composition;

using Amazon.AWSToolkit.Settings;

using AwsToolkit.VsSdk.Common.Settings.Proxy;

namespace Amazon.AwsToolkit.CodeWhisperer.Settings
{
    [Export(typeof(IProxySettingsRepository))]
    internal class LspProxySettingsRepository : IProxySettingsRepository
    {
        private readonly IProxySettingsRepository _repository = new ProxySettingsRepository();

        [ImportingConstructor]
        public LspProxySettingsRepository()
        {
        }

        public ProxySettings Get()
        {
            return _repository.Get();
        }

        public void Save(ProxySettings settings)
        {
            _repository.Save(settings);
        }
    }
}
