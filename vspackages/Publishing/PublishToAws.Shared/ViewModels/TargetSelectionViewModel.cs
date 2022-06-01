using Amazon.AWSToolkit.Publish.Models;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    /// <summary>
    /// Represents a Target Selection mode in the Publish to AWS experience.
    /// </summary>
    public abstract class TargetSelectionViewModel
    {
        /// <summary>
        /// A user-facing name for the operation associated with this mode
        /// </summary>
        public string DisplayName
        {
            get;
            protected set;
        }

        /// <summary>
        /// Type of target selection that is surfaced in the Publish to AWS experience
        /// </summary>
        public TargetSelectionMode TargetSelectionMode
        {
            get;
            protected set;
        }

        /// <summary>
        /// The currently selected target for this mode
        /// </summary>
        public PublishDestinationBase SelectedTarget;

        protected TargetSelectionViewModel()
        {
            
        }
    }
}
