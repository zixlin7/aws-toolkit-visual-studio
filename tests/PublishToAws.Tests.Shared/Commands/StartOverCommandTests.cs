using System.Linq;

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
        [InlineData(TargetSelectionMode.ExistingTargets, true, TargetSelectionMode.ExistingTargets)]
        [InlineData(TargetSelectionMode.ExistingTargets, false, TargetSelectionMode.ExistingTargets)]
        [InlineData(TargetSelectionMode.NewTargets, true, TargetSelectionMode.ExistingTargets)]
        [InlineData(TargetSelectionMode.NewTargets, false, TargetSelectionMode.NewTargets)]
        public async Task ExecuteCommand(TargetSelectionMode initialSelectionMode, bool publishSuccess, TargetSelectionMode expectedSelectionMode)
        {
            ViewModel.SessionId = "old-session-id";
            await ViewModel.SetTargetSelectionModeAsync(initialSelectionMode, _commandFixture.CancellationToken);
            ViewModel.PublishDestination = initialSelectionMode == TargetSelectionMode.ExistingTargets
                ? ViewModel.RepublishTargets.First<PublishDestinationBase>()
                : ViewModel.Recommendations.First<PublishDestinationBase>();
            ViewModel.PublishProjectViewModel.ProgressStatus = publishSuccess ? ProgressStatus.Success : ProgressStatus.Fail;
            await _sut.ExecuteAsync(null);

            Assert.Equal(PublishViewStage.Target, ViewModel.ViewStage);
            Assert.Equal(ProgressStatus.Loading, ViewModel.PublishProjectViewModel.ProgressStatus);
            Assert.Equal(SampleSessionId, ViewModel.SessionId);
            Assert.Equal(expectedSelectionMode, ViewModel.GetTargetSelectionMode());
            Assert.NotEmpty(ViewModel.Recommendations);
            Assert.NotEmpty(ViewModel.RepublishTargets);
            Assert.Empty(ViewModel.PublishProjectViewModel.DeploymentMessages);
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
