using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Install;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Publish.Services;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.Settings.Publish;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Install
{
    public class CliServerFactoryTests
    {
        private readonly SpyOutputToolkitShellProvider _toolkitHost = new SpyOutputToolkitShellProvider();
        private readonly IPublishSettingsRepository
            _publishSettingsRepository = new InMemoryPublishSettingsRepository();
        private readonly PublishSettings _publishSettings = PublishSettings.CreateDefault();
        private readonly string _installToolPath = @"my\cool\path";
        private readonly string _alternateToolPath = @"my\abc\path";
        private readonly string _additionalArguments = "-p param -c clear";
        private readonly InstallOptions _installOptions;

        public CliServerFactoryTests()
        {
            _installOptions = new InstallOptions(_installToolPath, "0.1.*");
        }

        [Fact]
        public async Task ShouldUseDefaultSettings()
        {
            _publishSettingsRepository.Save(_publishSettings);

            await CliServerFactory.CreateAsync(_installOptions, _publishSettingsRepository, _toolkitHost);

            Assert.True(string.IsNullOrEmpty( _toolkitHost.Message));
        }

        [Fact]
        public async Task ShouldUseAlternateCliPath()
        {
            var alternateDeployServerSettings = DeployServerSettings.CreateDefault();
            alternateDeployServerSettings.AlternateCliPath = _alternateToolPath;
            SaveUpdatedSettings(alternateDeployServerSettings);

            await CliServerFactory.CreateAsync(_installOptions, _publishSettingsRepository, _toolkitHost);

            Assert.Contains(_alternateToolPath, _toolkitHost.Message);
        }

        [Fact]
        public async Task ShouldUseLoggingEnabledOverride()
        {
            var alternateDeployServerSettings = DeployServerSettings.CreateDefault();
            alternateDeployServerSettings.LoggingEnabled = true;
            SaveUpdatedSettings(alternateDeployServerSettings);

            await CliServerFactory.CreateAsync(_installOptions, _publishSettingsRepository, _toolkitHost);

            Assert.Contains("enabled", _toolkitHost.Message);
        }

        [Fact]
        public async Task ShouldUseAdditionalArguments()
        {
            var alternateDeployServerSettings = DeployServerSettings.CreateDefault();
            alternateDeployServerSettings.AdditionalArguments = _additionalArguments;
            SaveUpdatedSettings(alternateDeployServerSettings);

            await CliServerFactory.CreateAsync(_installOptions, _publishSettingsRepository, _toolkitHost);

            Assert.Contains(_additionalArguments, _toolkitHost.Message);
        }

        private void SaveUpdatedSettings(DeployServerSettings deployServerSettings)
        {
            _publishSettings.DeployServer = deployServerSettings;
            _publishSettingsRepository.Save(_publishSettings);
        }
    }

    public class SpyOutputToolkitShellProvider : NoOpToolkitShellProvider
    {
        public string Message { get; private set; }

        public override void OutputToHostConsole(string message, bool forceVisible)
        {
            Message = message;
        }
    }
}
