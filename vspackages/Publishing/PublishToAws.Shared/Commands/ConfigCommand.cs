using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Commands
{
    /// <summary>
    /// WPF command binding for Publish Document Panel's Edit Settings Button
    /// </summary>
    public class ConfigCommand : TargetViewCommand
    {
        public ConfigCommand(PublishToAwsDocumentViewModel viewModel) : base(viewModel) {}

        protected override async Task ExecuteCommandAsync()
        {
            await PublishDocumentViewModel.JoinableTaskFactory.SwitchToMainThreadAsync();
            PublishDocumentViewModel.ViewStage = PublishViewStage.Configure;
        }

        protected override bool CanExecuteCommand()
        {
            return PublishDocumentViewModel.ViewStage == PublishViewStage.Target &&
                   !PublishDocumentViewModel.IsLoading &&
                   !PublishDocumentViewModel.LoadingSystemCapabilities &&
                   !PublishDocumentViewModel.SystemCapabilities.Any();
        }
    }
}
