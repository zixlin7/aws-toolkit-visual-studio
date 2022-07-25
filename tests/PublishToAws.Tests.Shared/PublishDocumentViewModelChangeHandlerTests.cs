using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tests.Publishing.Common;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing
{
    public class PublishDocumentViewModelChangeHandlerTests
    {
        [Fact]
        public async Task ShouldRefreshTarget_Republish()
        {
            //arrange
            var viewModel = CreateViewModel();
            await viewModel.SetTargetSelectionModeAsync(TargetSelectionMode.ExistingTargets, CancellationToken.None);
            viewModel.PublishDestination = CreateRepublishTarget();
            AssertIsTargetRefreshNeeded(viewModel, true);
        }

        [Fact]
        public async Task ShouldRefreshTarget_NewPublish()
        {
            //arrange
            var viewModel = CreateViewModel();
            await viewModel.SetTargetSelectionModeAsync(TargetSelectionMode.NewTargets, CancellationToken.None);
            viewModel.PublishDestination = CreateNewPublishTarget();
            AssertIsTargetRefreshNeeded(viewModel, true);
        }

        private PublishToAwsDocumentViewModel CreateViewModel() => new PublishToAwsDocumentViewModel(new PublishApplicationContext(new PublishContextFixture().PublishContext));

        private RepublishTarget CreateRepublishTarget() => new RepublishTarget( null);

        private PublishRecommendation CreateNewPublishTarget() => new PublishRecommendation( null);

        private static void AssertIsTargetRefreshNeeded(PublishToAwsDocumentViewModel viewModel, bool expected)
        {
            //act
            var shouldRefresh = new PublishDocumentViewModelChangeHandler().IsTargetRefreshNeeded(viewModel);
            //assert
            Assert.Equal(expected, shouldRefresh);
        }

        [Fact]
        public async Task ShouldNotRefreshIfTargetIsNull()
        {
            //arrange
            var viewModel = CreateViewModel();
            await viewModel.SetTargetSelectionModeAsync(TargetSelectionMode.NewTargets, CancellationToken.None);
            viewModel.PublishDestination = null;
            AssertIsTargetRefreshNeeded(viewModel, false);
        }
    }
}
