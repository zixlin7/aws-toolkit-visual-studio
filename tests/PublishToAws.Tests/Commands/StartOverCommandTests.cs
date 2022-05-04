using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;

using Moq;

using Xunit;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class StartOverCommandTests
    {
        private static readonly string SampleSessionId = "session-id-1234";
        private readonly PublishFooterCommandFixture _commandFixture = new PublishFooterCommandFixture();
        private TestPublishToAwsDocumentViewModel ViewModel => _commandFixture.ViewModel;
        private readonly StartOverCommand _sut;

        public StartOverCommandTests()
        {
            _commandFixture.StubStartSessionToReturn(new SessionDetails()
            {
                DefaultApplicationName = "some-app",
                SessionId = SampleSessionId,
            });
            _commandFixture.StubGetRecommendationsAsync(SamplePublishData.CreateSampleRecommendations());
            _commandFixture.StubGetRepublishTargetsAsync(SamplePublishData.CreateSampleRepublishTargets());

            _sut = new StartOverCommand(ViewModel, null);
            ViewModel.Connection.Region = new ToolkitRegion()
            {
                Id = "us-west-2"
            };
            ViewModel.ViewStage = PublishViewStage.Publish;
            ViewModel.PublishProjectViewModel.IsPublishing = false;
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, false, false)]
        public void ExecuteCommand(bool initialIsRepublish, bool publishSuccess, bool expectedIsRepublish)
        {
            ViewModel.SessionId = "old-session-id";
            ViewModel.IsRepublish = initialIsRepublish;
            ViewModel.PublishProjectViewModel.ProgressStatus = publishSuccess ? ProgressStatus.Success : ProgressStatus.Fail;
            _sut.Execute(null);

            Assert.Equal(PublishViewStage.Target, ViewModel.ViewStage);
            Assert.Equal(ProgressStatus.Loading, ViewModel.PublishProjectViewModel.ProgressStatus);
            Assert.Equal(SampleSessionId, ViewModel.SessionId);
            Assert.Equal(expectedIsRepublish, ViewModel.IsRepublish);
            Assert.NotEmpty(ViewModel.Recommendations);
            Assert.NotEmpty(ViewModel.RepublishTargets);
            Assert.Empty(ViewModel.PublishProjectViewModel.PublishProgress);
        }

        [Fact]
        public async Task ExecuteCommandFails()
        {
            _commandFixture.StubStartSessionThrows();

            await _sut.ExecuteAsync(null);

            _commandFixture.ShellProvider.Verify(x => x.OutputToHostConsole(It.Is<string>(s => s.Contains("Failed to reset the Publish to AWS view")), true), Times.Once);

            Assert.NotEqual(PublishViewStage.Target, ViewModel.ViewStage);
        }

        [Fact]
        public void CanExecute()
        {
            ViewModel.ViewStage = PublishViewStage.Publish;
            Assert.True(_sut.CanExecute(null));
        }

        [Theory]
        [InlineData(PublishViewStage.Target)]
        [InlineData(PublishViewStage.Configure)]
        public void CanExecute_NotCorrectView(PublishViewStage viewStage)
        {
            ViewModel.ViewStage = viewStage;
            Assert.False(_sut.CanExecute(null));
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void CanExecute_PublishStage(bool isPublishing, bool expectedCanExecute)
        {
            ViewModel.ViewStage = PublishViewStage.Publish;
            ViewModel.PublishProjectViewModel.IsPublishing = isPublishing;
            Assert.Equal(expectedCanExecute, _sut.CanExecute(null));
        }
    }
}
