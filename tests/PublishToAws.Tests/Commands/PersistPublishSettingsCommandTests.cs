using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Tests.Common.Settings.Publish;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class PersistBannerVisibilityCommandTests
    {
        private readonly PublishFooterCommandFixture _commandFixture = new PublishFooterCommandFixture();
        private TestPublishToAwsDocumentViewModel ViewModel => _commandFixture.ViewModel;
        private readonly IPublishSettingsRepository _settingsRepository = new InMemoryPublishSettingsRepository();
        private readonly PersistBannerVisibilityCommand _command;

        public PersistBannerVisibilityCommandTests()
        {
            _command = new PersistBannerVisibilityCommand(_settingsRepository, ViewModel);
        }

        [Fact]
        public async Task Execute_ShouldPersistSettings()
        {
            // arrange.
            var expectedSettings = new PublishSettings {ShowPublishBanner = false};

            // act.
            _command.Execute(null);

            // assert
            var actualSettings = await _settingsRepository.GetAsync();
            Assert.Equal(expectedSettings, actualSettings);
        }

        [Fact]
        public void Execute_ShouldUpdateIsOptionsEnabled()
        {
            // arrange.
            ViewModel.IsOptionsBannerEnabled = true;

            // act.
            _command.Execute(null);

            Assert.False(ViewModel.IsOptionsBannerEnabled);
        }

        [Fact]
        public async Task Execute_ShouldNotOverrideExistingSettings()
        {
            // arrange.
            var existingSettings = new PublishSettings() {DeployServer = new DeployServerSettings(new PortRange(1, 3))};
            _settingsRepository.Save(existingSettings);

            var expectedSettings =
                new PublishSettings {DeployServer = existingSettings.DeployServer, ShowPublishBanner = false};

            // act.
            _command.Execute(null);

            // assert
            var actualSettings = await _settingsRepository.GetAsync();
            Assert.Equal(expectedSettings, actualSettings);
        }
    }
}
