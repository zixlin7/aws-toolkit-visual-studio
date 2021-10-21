using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Shared;

using EnvDTE;

using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Publishing
{
    /// <summary>
    /// Command that backs the "Publish to AWS" menu item
    /// that appears in the Solution Explorer context menu.
    /// </summary>
    public class PublishToAwsCommandSolutionExplorer : BasePublishToAwsCommand<PublishToAwsCommandSolutionExplorer>
    {
        public PublishToAwsCommandSolutionExplorer(ToolkitContext toolkitContext,
            IAWSToolkitShellProvider toolkitShell,
            IPublishSettingsRepository publishSettingsRepository)
            : base(toolkitContext, toolkitShell, publishSettingsRepository)
        {
        }

        /// <summary>
        /// Overload for testing purposes
        /// </summary>
        public PublishToAwsCommandSolutionExplorer(
            ToolkitContext toolkitContext,
            IAWSToolkitShellProvider toolkitShell,
            IPublishSettingsRepository publishSettingsRepository,
            DTE dte,
            IVsMonitorSelection monitorSelection,
            IVsSolution solution,
            IPublishToAws publishToAws)
            : base(toolkitContext, toolkitShell, publishSettingsRepository, dte, monitorSelection, solution, publishToAws)
        {
        }

        protected override string GetMenuText(Project project)
        {
            return "Publish to AWS (Preview feature)...";
        }
    }
}
