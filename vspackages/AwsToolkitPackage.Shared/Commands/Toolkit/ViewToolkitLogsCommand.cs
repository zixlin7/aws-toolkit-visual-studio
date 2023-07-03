using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;

using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Toolkit
{
    /// <summary>
    /// Extension command responsible for viewing location of the toolkit logs
    /// </summary>
    public class ViewToolkitLogsCommand : BaseCommand<ViewToolkitLogsCommand>
    {
        private readonly OpenToolkitLogsCommand _wrappedCommand;

        public ViewToolkitLogsCommand(ToolkitContext toolkitContext)
        {
            _wrappedCommand = new OpenToolkitLogsCommand(toolkitContext);
        }

        public static Task<ViewToolkitLogsCommand> InitializeAsync(
            ToolkitContext toolkitContext,
            Guid menuGroup, int commandId,
            AsyncPackage package)
        {
            return InitializeAsync(
                () => new ViewToolkitLogsCommand(toolkitContext),
                menuGroup, commandId,
                package);
        }

        protected override void Execute(object sender, EventArgs args)
        {
            _wrappedCommand.Execute(null);
        }

        protected override void BeforeQueryStatus(OleMenuCommand menuCommand, EventArgs e)
        {
            try
            {
                menuCommand.Visible = true;
            }
            catch
            {
                // Swallow error for stability -- menu will not be visible
                // do not log - this is invoked each time the menu is opened
            }
        }
    }
}
