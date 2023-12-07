using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class ConfigCommandTests
    {
        private readonly PublishFooterCommandFixture _commandFixture = new PublishFooterCommandFixture();
        private TestPublishToAwsDocumentViewModel ViewModel => _commandFixture.ViewModel;
        private readonly ConfigCommand _sut;

        public ConfigCommandTests()
        {
            _sut = new ConfigCommand(ViewModel);
        }

        [Fact]
        public async Task ExecuteCommand_NewPublish()
        {
            await _commandFixture.SetupNewPublishAsync();
            await _sut.ExecuteAsync(null);

            Assert.Equal(PublishViewStage.Configure, ViewModel.ViewStage);
        }

        [Fact]
        public async Task ExecuteCommand_Republish()
        {
            await _commandFixture.SetupRepublishAsync();
            await _sut.ExecuteAsync(null);

            Assert.Equal(PublishViewStage.Configure, ViewModel.ViewStage);
        }

        [Fact]
        public async Task CanExecute_Republish()
        {
            await _commandFixture.SetupRepublishAsync();

            Assert.True(_sut.CanExecute(null));
        }

        [Fact]
        public async Task CanExecute_NewPublish()
        {
            await _commandFixture.SetupNewPublishAsync();

            Assert.True(_sut.CanExecute(null));
        }

        [Fact]
        public async Task CanExecute_IsLoading()
        {
            await _commandFixture.SetupNewPublishAsync();
            _commandFixture.ViewModel.IsLoading = true;

            Assert.False(_sut.CanExecute(null));
        }

        [Fact]
        public async Task CanExecute_MissingRequirement()
        {
            await _commandFixture.SetupNewPublishAsync();
            _commandFixture.AddMissingRequirement("some-requirement");

            Assert.False(_sut.CanExecute(null));
        }

        [Theory]
        [InlineData(PublishViewStage.Configure)]
        [InlineData(PublishViewStage.Publish)]
        public void CanExecute_NotCorrectView(PublishViewStage viewStage)
        {
            ViewModel.ViewStage = viewStage;
            Assert.False(_sut.CanExecute(null));
        }
    }
}
