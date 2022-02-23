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
        public void ExecuteCommand_NewPublish()
        {
            _commandFixture.SetupNewPublish();
            _sut.Execute(null);

            Assert.Equal(PublishViewStage.Configure, ViewModel.ViewStage);
        }

        [Fact]
        public void ExecuteCommand_Republish()
        {
            _commandFixture.SetupRepublish();
            _sut.Execute(null);

            Assert.Equal(PublishViewStage.Configure, ViewModel.ViewStage);
        }

        [Fact]
        public void CanExecute_Republish()
        {
            _commandFixture.SetupRepublish();

            Assert.True(_sut.CanExecute(null));
        }

        [Fact]
        public void CanExecute_NewPublish()
        {
            _commandFixture.SetupNewPublish();

            Assert.True(_sut.CanExecute(null));
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
