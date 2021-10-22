using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Shared;

using EnvDTE;

using EnvDTE80;

using log4net;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Publishing
{
    /// <summary>
    /// Command that backs the "Publish to AWS" menu item
    /// that appears in the Build menu.
    /// </summary>
    public class PublishToAwsCommand : BasePublishToAwsCommand<PublishToAwsCommand>
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(PublishToAwsCommand));

        public PublishToAwsCommand(ToolkitContext toolkitContext,
            IAWSToolkitShellProvider toolkitShell,
            IPublishSettingsRepository publishSettingsRepository)
            : base(toolkitContext, toolkitShell, publishSettingsRepository)
        {
        }

        /// <summary>
        /// Overload for testing purposes
        /// </summary>
        public PublishToAwsCommand(
            ToolkitContext toolkitContext,
            IAWSToolkitShellProvider toolkitShell,
            IPublishSettingsRepository publishSettingsRepository,
            DTE2 dte,
            IVsMonitorSelection monitorSelection,
            IVsSolution solution,
            IPublishToAws publishToAws)
            : base(toolkitContext, toolkitShell, publishSettingsRepository, dte, monitorSelection, solution, publishToAws)
        {
        }

        protected override string GetMenuText(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return $"Publish {project.Name} to AWS (Preview feature)...";
        }
    }
}
