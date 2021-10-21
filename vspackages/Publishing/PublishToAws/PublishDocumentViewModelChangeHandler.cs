using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish
{
    public class PublishDocumentViewModelChangeHandler
    {
        public bool ShouldRefreshTarget(PublishToAwsDocumentViewModel viewModel)
        {
            return viewModel.IsRepublish ? viewModel.RepublishTarget != null : viewModel.Recommendation != null;
        }
    }
}
