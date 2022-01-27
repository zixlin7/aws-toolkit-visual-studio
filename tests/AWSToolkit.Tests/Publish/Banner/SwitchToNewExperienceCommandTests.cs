using System.Threading.Tasks;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish.Banner;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Solutions;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.Settings.Publish;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Publish.Banner
{
    public class SwitchToNewExperienceCommandTests
    {
        public class SpyPublishToAws : IPublishToAws
        {
            public ShowPublishToAwsDocumentArgs Args { get; private set; }

            public Task ShowPublishToAwsDocument(ShowPublishToAwsDocumentArgs args)
            {
                Args = args;

                return Task.CompletedTask;
            }
        }

        private readonly IAwsConnectionManager _connectionManager = ConnectionManagerFixture.Create();
        private readonly Project _project = ProjectFixture.Create();
        private readonly PublishBannerViewModel _publishBanner;

        private readonly SpyPublishToAws _spyPublishToAws = new SpyPublishToAws();

        private readonly IPublishSettingsRepository _settingsRepository = new InMemoryPublishSettingsRepository();

        private readonly IAsyncCommand _switchToNewExperienceCommand;
        public Mock<ITelemetryLogger> TelemetryLogger { get; } = new Mock<ITelemetryLogger>();

        public SwitchToNewExperienceCommandTests()
        {
            _publishBanner = new PublishBannerViewModel(CreateToolkitContext(), _settingsRepository);
            _switchToNewExperienceCommand = SwitchToNewExperienceCommandFactory.Create(_publishBanner, _spyPublishToAws);
        }

        private ToolkitContext CreateToolkitContext()
        {
            var toolkitHost = new ProjectToolkitShellProvider(_project);
            return new ToolkitContext { ToolkitHost = toolkitHost, ConnectionManager = _connectionManager,
                TelemetryLogger = TelemetryLogger.Object};
        }

        [Fact]
        public async Task ShouldSetCurrentPublishExperienceToBeClosed()
        {
            // act.
            await _switchToNewExperienceCommand.ExecuteAsync(null);

            // assert.
            Assert.True(_publishBanner.CloseCurrentPublishExperience);
        }

        [Fact]
        public async Task ShouldOpenNewPublishExperience()
        {
            // arrange.
            var expectedArgs = new ShowPublishToAwsDocumentArgs
            {
                ProjectName = _project.Name,
                ProjectPath = _project.Path,
                CredentialId = _connectionManager.ActiveCredentialIdentifier,
                Region = _connectionManager.ActiveRegion
            };

            // act.
            await _switchToNewExperienceCommand.ExecuteAsync(null);

            // assert.
            Assert.Equal(expectedArgs, _spyPublishToAws.Args);
        }
    }
}
