using System.Linq;

using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Commands
{
    /// <summary>
    ///  Wrapper for enabling WPF command binding for Publish Document Panel's Target View
    /// </summary>
    public abstract class TargetViewCommand : PublishFooterCommand
    {
        protected TargetViewCommand(PublishToAwsDocumentViewModel viewModel) : base(viewModel)
        {
        }

        public override bool CanExecute(object parameter)
        {
            // TODO : one final verification call to the API to see that our deployment session is valid
            if (!PublishDocumentViewModel.IsSessionEstablished || !PublishDocumentViewModel.IsStackNameSet)
            {
                return false;
            }

            if (PublishDocumentViewModel.IsRepublish && !CanExecuteForRepublishView())
            {
                return false;
            }

            if (!PublishDocumentViewModel.IsRepublish && !CanExecuteForPublishView())
            {
                return false;
            }

            return CanExecuteCommand();
        }

        /// <summary>
        /// Evaluates whether ICommand can be executed based on properties specific to derived classes (for eg. Publish Command, Edit Settings/Config Command)
        /// </summary>
        protected abstract bool CanExecuteCommand();

        private bool CanExecuteForPublishView()
        {
            return HasRecommendations() && PublishDocumentViewModel.Recommendation != null;
        }

        private bool CanExecuteForRepublishView()
        {
            return HasRepublishTargets() && PublishDocumentViewModel.RepublishTarget != null;
        }

        private bool HasRecommendations()
        {
            return PublishDocumentViewModel.Recommendations != null &&
                   PublishDocumentViewModel.Recommendations.Any();
        }

        private bool HasRepublishTargets()
        {
            return PublishDocumentViewModel.RepublishTargets != null &&
                   PublishDocumentViewModel.RepublishTargets.Any();
        }
    }
}
