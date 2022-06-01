using Amazon.AWSToolkit.Publish.Models;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    /// <summary>
    /// Represents the selection of a target to perform a new publish
    /// </summary>
    public class NewPublishTargetsViewModel : TargetSelectionViewModel
    {
        public NewPublishTargetsViewModel()
        {
            DisplayName = "Publish to New Target";
            TargetSelectionMode = TargetSelectionMode.NewTargets;
        }
    }
}
