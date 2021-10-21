using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Publish.Views;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.Settings.Publish;
using Amazon.AWSToolkit.Tests.Publishing.Common;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class ReenableOldPublishCommandTests
    {
        private readonly IPublishSettingsRepository _settingsRepository = new InMemoryPublishSettingsRepository();
        private readonly SpyShowInModalToolkitShellProvider _spyToolkitHost = new SpyShowInModalToolkitShellProvider();
        private readonly PublishContextFixture _contextFixture = new PublishContextFixture();
        private readonly ICommand _reenableOldPublishCommand;

        public ReenableOldPublishCommandTests()
        {
            _contextFixture.PublishContext.ToolkitShellProvider = _spyToolkitHost;
            _contextFixture.PublishContext.PublishSettingsRepository = _settingsRepository;
            var viewModel = new TestPublishToAwsDocumentViewModel(new PublishApplicationContext(_contextFixture.PublishContext));
            _reenableOldPublishCommand =
                ReenableOldPublishCommandFactory.Create(viewModel);
        }

        [StaFact]
        public void ShouldShowInModalDialog()
        {
            _reenableOldPublishCommand.Execute(null);

            Assert.Equal(MessageBoxButton.OK, _spyToolkitHost.Button);
            Assert.NotNull(_spyToolkitHost.HostedControl);
            Assert.IsType<ReenableOldPublishDialog>(_spyToolkitHost.HostedControl);
        }

        [Fact]
        public async Task ShouldPersistOldPublishVisibilitySettingsAsync()
        {
            var existingSettings = PublishSettings.CreateDefault();
            existingSettings.ShowOldPublishExperience = false;

            _settingsRepository.Save(existingSettings);

            var expectedSettings = PublishSettings.CreateDefault();
            expectedSettings.ShowOldPublishExperience = true;

            _reenableOldPublishCommand.Execute(null);

            var actualSettings = await _settingsRepository.GetAsync();

            Assert.Equal(expectedSettings, actualSettings);
        }

        public class SpyShowInModalToolkitShellProvider : NoOpToolkitShellProvider
        {
            public MessageBoxButton Button { get; private set; }
            public IAWSToolkitControl HostedControl { get; private set; }

            public override bool ShowInModalDialogWindow(IAWSToolkitControl hostedControl, MessageBoxButton button)
            {
                Button = button;
                HostedControl = hostedControl;
                return true;
            }
        }
    }
}
