using Amazon.AWSToolkit.Publish.Models;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    /// <summary>
    /// Represents the selection of a target to perform a re-publish
    /// </summary>
    public class ExistingPublishTargetsViewModel : TargetSelectionViewModel
    {
        public ExistingPublishTargetsViewModel()
        {
            DisplayName = "Publish to Existing Target";
            TargetSelectionMode = TargetSelectionMode.ExistingTargets;
        }
    }
}
