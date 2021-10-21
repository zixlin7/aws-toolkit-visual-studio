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
            Assert.True(_sut.CanExecute(null));
        }

        [Fact]
        public void CanExecute_WhenRepublishView()
        {
            SetupRepublishView();
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
            ViewModel.Recommendation = null;
            Assert.False(_sut.CanExecute(null));
        }

        [Fact]
        public void CanExecute_NullRecommendationsWhenPublishView()
        {
            ViewModel.Recommendations = null;
            Assert.False(_sut.CanExecute(null));
        }

        [Fact]
        public void CanExecute_EmptyRecommendationsWhenPublishView()
        {
            ViewModel.Recommendations.Clear();
            Assert.False(_sut.CanExecute(null));
        }

        [Fact]
        public void CanExecute_NullRepublishTargetWhenRepublishView()
        {
            ViewModel.RepublishTarget = null;
            AssertRepublishViewCanNotExecute();
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

        private void SetupRepublishView()
        {
            ViewModel.IsRepublish = true;
        }

        private void AssertRepublishViewCanNotExecute()
        {
            SetupRepublishView();
            Assert.False(_sut.CanExecute(null));
        }
    }
}
