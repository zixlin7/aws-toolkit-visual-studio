using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish
{
    public class PublishDocumentViewModelChangeHandler
    {
        public bool IsTargetRefreshNeeded(PublishToAwsDocumentViewModel viewModel)
        {
            var mode = viewModel.GetTargetSelectionMode();

            return (mode == TargetSelectionMode.NewTargets && viewModel.PublishDestination is PublishRecommendation) ||
                   (mode == TargetSelectionMode.ExistingTargets && viewModel.PublishDestination is RepublishTarget);
        }
    }
}
