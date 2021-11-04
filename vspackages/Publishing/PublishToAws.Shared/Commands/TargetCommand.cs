using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Commands
{
    /// <summary>
    /// WPF command binding for Publish Document Panel's Back to Target Button
    /// </summary>
    public class TargetCommand : PublishFooterCommand
    {
        public TargetCommand(PublishToAwsDocumentViewModel viewModel) : base(viewModel) { }

        public override bool CanExecute(object paramter)
        {
            return PublishDocumentViewModel.ViewStage == PublishViewStage.Configure;
        }

        protected override async Task ExecuteCommandAsync()
        {
            await PublishDocumentViewModel.JoinableTaskFactory.SwitchToMainThreadAsync();
            PublishDocumentViewModel.ViewStage = PublishViewStage.Target;
        }
    }
}
