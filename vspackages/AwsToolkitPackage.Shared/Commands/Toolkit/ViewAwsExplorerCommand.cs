using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Toolkit
{
    /// <summary>
    /// Menu command responsible for showing the AWS Explorer
    /// </summary>
    public class ViewAwsExplorerCommand : BaseCommand<ViewAwsExplorerCommand>
    {
        private readonly OpenAwsExplorerCommand _wrappedCommand;

        public ViewAwsExplorerCommand(ToolkitContext toolkitContext)
        {
            _wrappedCommand = new OpenAwsExplorerCommand(toolkitContext);
        }

        public static Task<ViewAwsExplorerCommand> InitializeAsync(
            ToolkitContext toolkitContext,
            Guid menuGroup, int commandId,
            AsyncPackage package)
        {
            return InitializeAsync(
                () => new ViewAwsExplorerCommand(toolkitContext),
                menuGroup, commandId,
                package);
        }

        protected override Task ExecuteAsync(object sender, OleMenuCmdEventArgs args)
        {
            return _wrappedCommand.ExecuteAsync();
        }
    }
}
