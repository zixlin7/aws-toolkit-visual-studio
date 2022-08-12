using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.VisualStudio.ToolWindow;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Toolkit
{
    /// <summary>
    /// Menu command responsible for showing the AWS Explorer
    /// </summary>
    public class ViewAwsExplorerCommand : BaseCommand<ViewAwsExplorerCommand>
    {
        public static Task<ViewAwsExplorerCommand> InitializeAsync(
            Guid menuGroup, int commandId,
            AsyncPackage package)
        {
            return InitializeAsync(
                () => new ViewAwsExplorerCommand(package),
                menuGroup, commandId,
                package);
        }

        private readonly AsyncPackage _package;

        public ViewAwsExplorerCommand(AsyncPackage package)
        {
            _package = package;
        }

        protected override async Task ExecuteAsync(object sender, OleMenuCmdEventArgs args)
        {
            await _package.ShowToolWindowAsync(
                typeof(AWSNavigatorToolWindow),
                0,
                true,
                _package.DisposalToken);
        }
    }
}
