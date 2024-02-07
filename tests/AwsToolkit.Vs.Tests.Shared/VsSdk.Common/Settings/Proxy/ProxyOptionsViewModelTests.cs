using Amazon.AWSToolkit.Settings;

using AwsToolkit.VsSdk.Common.Settings.Proxy;

using Xunit;

namespace AwsToolkit.Vs.Tests.VsSdk.Common.Settings.Proxy
{
    public class FakeProxySettingsRepository : IProxySettingsRepository
    {
        public ProxySettings Get()
        {
            return ProxySettings;
        }

        public void Save(ProxySettings settings)
        {
           ProxySettings = settings;
        }

        public ProxySettings ProxySettings { get; private set; }
    }

    public class ProxyOptionsViewModelTests
    {
        private readonly FakeProxySettingsRepository _repository = new FakeProxySettingsRepository();
        private readonly ProxySettings _sampleSettings = new ProxySettings() { Host = "127.0.0.1", Port = 4444 };
        private readonly ProxyOptionsViewModel _sut;

        public ProxyOptionsViewModelTests()
        {
            _sut = new ProxyOptionsViewModel(_repository);
        }

        [Fact]
        public void Load_WhenSettingsNull()
        {
            _sut.Load();

            Assert.Null(_sut.ProxySettings);
        }

        [Fact]
        public void Load()
        {
            _repository.Save(_sampleSettings);

            _sut.Load();

            Assert.Equal(_repository.ProxySettings, _sut.ProxySettings);
        }

        [Fact]
        public void Save()
        {
            Assert.Null(_sut.ProxySettings);

            _sut.ProxySettings = _sampleSettings;
            _sut.Save();

            Assert.Equal(_sut.ProxySettings, _repository.ProxySettings);
        }

        [Fact]
        public void Save_WhenExistingSettings()
        {
            _repository.Save(_sampleSettings);
            Assert.Null(_repository.ProxySettings.Username);

            _sampleSettings.Username = "abc";
            _sut.ProxySettings = _sampleSettings;

            _sut.Save();

            Assert.Equal(_sut.ProxySettings, _repository.ProxySettings);
            Assert.Equal("abc", _repository.ProxySettings.Username);
        }
    }
}
