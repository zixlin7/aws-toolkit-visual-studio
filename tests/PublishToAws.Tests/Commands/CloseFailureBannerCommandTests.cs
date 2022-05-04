using System.Threading.Tasks;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class CloseFailureBannerCommandTests
    {
        private readonly PublishFooterCommandFixture _commandFixture = new PublishFooterCommandFixture();
        private PublishProjectViewModel ViewModel => _commandFixture.ViewModel.PublishProjectViewModel;
        private readonly IAsyncCommand _command;

        public CloseFailureBannerCommandTests()
        {
            _command = CloseFailureBannerCommandFactory.Create(ViewModel, _commandFixture.JoinableTaskFactory);
        }

        [Fact]
        public async Task Execute_ShouldUpdateIsFailureBannerEnabled()
        {
            //arrange
            ViewModel.IsFailureBannerEnabled = true;

            //act
            await _command.ExecuteAsync(null);

            //assert
            Assert.False(ViewModel.IsFailureBannerEnabled);
        }
    }
}
