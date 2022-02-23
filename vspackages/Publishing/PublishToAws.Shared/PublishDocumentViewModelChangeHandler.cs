using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish
{
    public class PublishDocumentViewModelChangeHandler
    {
        public bool ShouldRefreshTarget(PublishToAwsDocumentViewModel viewModel)
        {
            return viewModel.IsRepublish
                ? viewModel.PublishDestination is RepublishTarget
                : viewModel.PublishDestination is PublishRecommendation;
        }
    }
}
