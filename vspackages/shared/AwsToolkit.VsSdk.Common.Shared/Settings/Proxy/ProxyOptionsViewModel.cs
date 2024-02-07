using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Settings;

namespace AwsToolkit.VsSdk.Common.Settings.Proxy
{
    public class ProxyOptionsViewModel : BaseModel
    {
        private readonly IProxySettingsRepository _repository;
        private ProxySettings _proxySettings;

        public ProxyOptionsViewModel() : this(new ProxySettingsRepository())
        {
        }

        public ProxyOptionsViewModel(IProxySettingsRepository repository)
        {
            _repository = repository;
        }

        public ProxySettings ProxySettings
        {
            get => _proxySettings;
            set => SetProperty(ref _proxySettings, value);
        }

        public void Load()
        {
            ProxySettings = _repository.Get();
        }

        public void Save()
        {
            _repository.Save(ProxySettings);
        }

    }
}
