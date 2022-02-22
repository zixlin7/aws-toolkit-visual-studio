using System.Linq;

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
        public void CanExecute_WhenPublishView()
        {
            _commandFixture.SetupNewPublish();
            Assert.True(_sut.CanExecute(null));
        }

        [Fact]
        public void CanExecute_WhenRepublishView()
        {
            _commandFixture.SetupRepublish();
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
        public void CanExecute_NullRecommendationWhenPublishView()
        {
            _commandFixture.SetupNewPublish();
            ViewModel.PublishDestination = null;
            Assert.False(_sut.CanExecute(null));
        }

        [Fact]
        public void CanExecute_NullRecommendationsWhenPublishView()
        {
            _commandFixture.SetupNewPublish();
            ViewModel.Recommendations = null;
            Assert.False(_sut.CanExecute(null));
        }

        [Fact]
        public void CanExecute_EmptyRecommendationsWhenPublishView()
        {
            _commandFixture.SetupNewPublish();
            ViewModel.Recommendations.Clear();
            Assert.False(_sut.CanExecute(null));
        }

        [Fact]
        public void CanExecute_NullRepublishTargetWhenRepublishView()
        {
            _commandFixture.SetupRepublish();
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

        private void AssertRepublishViewCanNotExecute()
        {
            _commandFixture.SetupRepublish();
            Assert.False(_sut.CanExecute(null));
        }
    }
}
