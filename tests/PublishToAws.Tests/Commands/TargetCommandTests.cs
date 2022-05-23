using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class TargetCommandTests
    {
        private readonly PublishFooterCommandFixture _commandFixture = new PublishFooterCommandFixture();
        private TestPublishToAwsDocumentViewModel ViewModel => _commandFixture.ViewModel;
        private readonly TargetCommand _sut;

        public TargetCommandTests()
        {
            _sut = new TargetCommand(ViewModel);
        }

        [Fact]
        public void ExecuteCommand()
        {
            ViewModel.ViewStage = PublishViewStage.Configure;
            _sut.Execute(null);

            Assert.Equal(PublishViewStage.Target, ViewModel.ViewStage);
        }
    }
}
