using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class TargetViewCommandTests
    {
        private readonly PublishFooterCommandFixture _commandFixture = new PublishFooterCommandFixture();
        private TestPublishToAwsDocumentViewModel ViewModel => _commandFixture.ViewModel;
        private readonly TargetViewCommand _sut;

        public TargetViewCommandTests()
        {
            _sut = new TestTargetViewCommand(ViewModel);
        }

        [Fact]
        public async Task CanExecute_WhenPublishView()
        {
            await _commandFixture.SetupNewPublishAsync();
            Assert.True(_sut.CanExecute(null));
        }

        [Fact]
        public async Task CanExecute_WhenRepublishView()
        {
            await _commandFixture.SetupRepublishAsync();
            Assert.True(_sut.CanExecute(null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        public void CanExecute_EmptyApplicationName(string applicationName)
        {
            ViewModel.StackName = applicationName;
            Assert.False(_sut.CanExecute(null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        public void CanExecute_EmptySessionId(string sessionId)
        {
            ViewModel.SessionId = sessionId;
            Assert.False(_sut.CanExecute(null));
        }

        [Fact]
        public async Task CanExecute_NullRecommendationWhenPublishView()
        {
            await _commandFixture.SetupNewPublishAsync();
            ViewModel.PublishDestination = null;
            Assert.False(_sut.CanExecute(null));
        }

        [Fact]
        public async Task CanExecute_NullRecommendationsWhenPublishView()
        {
            await _commandFixture.SetupNewPublishAsync();
            ViewModel.Recommendations = null;
            Assert.False(_sut.CanExecute(null));
        }

        [Fact]
        public async Task CanExecute_EmptyRecommendationsWhenPublishView()
        {
            await _commandFixture.SetupNewPublishAsync();
            ViewModel.Recommendations.Clear();
            Assert.False(_sut.CanExecute(null));
        }

        [Fact]
        public async Task CanExecute_NullRepublishTargetWhenRepublishView()
        {
            await _commandFixture.SetupRepublishAsync();
            ViewModel.PublishDestination = null;
            Assert.False(_sut.CanExecute(null));
        }

        [Fact]
        public void CanExecute_NullRepublishTargetsWhenRepublishView()
        {
            ViewModel.RepublishTargets = null;
            AssertRepublishViewCanNotExecute();
        }

        [Fact]
        public void CanExecute_EmptyRepublishTargetsWhenRepublishView()
        {
            ViewModel.RepublishTargets.Clear();
            AssertRepublishViewCanNotExecute();
        }

        [Fact]
        public void ViewModelPropertyChangeRaisesCanExecuteChanged()
        {
            bool raisedCanExecute = false;

            _sut.CanExecuteChanged += (sender, args) => { raisedCanExecute = true; };

            ViewModel.StackName = "stack-name";
            Assert.True(raisedCanExecute);
        }

        private async Task AssertRepublishViewCanNotExecute()
        {
            await _commandFixture.SetupRepublishAsync();
            Assert.False(_sut.CanExecute(null));
        }
    }
}
