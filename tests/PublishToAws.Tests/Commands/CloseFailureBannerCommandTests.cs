using System.Threading.Tasks;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class CloseFailureBannerCommandTests
    {
        private readonly PublishFooterCommandFixture _commandFixture = new PublishFooterCommandFixture();
        private TestPublishToAwsDocumentViewModel ViewModel => _commandFixture.ViewModel;
        private readonly IAsyncCommand _command;

        public CloseFailureBannerCommandTests()
        {
            _command = CloseFailureBannerCommandFactory.Create(ViewModel);
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
