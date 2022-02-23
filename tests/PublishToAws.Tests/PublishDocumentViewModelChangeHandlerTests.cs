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
        public void ShouldRefreshTarget_Republish()
        {
            //arrange
            var viewModel = CreateViewModel();
            viewModel.IsRepublish = true;
            viewModel.PublishDestination = CreateRepublishTarget();
            ShouldRefresh(viewModel, true);
        }

        [Fact]
        public void ShouldRefreshTarget_NewPublish()
        {
            //arrange
            var viewModel = CreateViewModel();
            viewModel.IsRepublish = false;
            viewModel.PublishDestination = CreateNewPublishTarget();
            ShouldRefresh(viewModel, true);
        }

        private PublishToAwsDocumentViewModel CreateViewModel() => new PublishToAwsDocumentViewModel(new PublishApplicationContext(new PublishContextFixture().PublishContext));

        private RepublishTarget CreateRepublishTarget() => new RepublishTarget( null);

        private PublishRecommendation CreateNewPublishTarget() => new PublishRecommendation( null);

        private static void ShouldRefresh(PublishToAwsDocumentViewModel viewModel, bool expected)
        {
            //act
            var shouldRefresh = new PublishDocumentViewModelChangeHandler().ShouldRefreshTarget(viewModel);
            //assert
            Assert.Equal(expected, shouldRefresh);
        }

        [Fact]
        public void ShouldNotRefreshIfTargetIsNull()
        {
            //arrange
            var viewModel = CreateViewModel();
            viewModel.IsRepublish = false;
            viewModel.PublishDestination = null;
            ShouldRefresh(viewModel, false);
        }
    }
}
