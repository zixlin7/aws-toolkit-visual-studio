using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Urls;

using log4net;

using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Toolkit
{
    /// <summary>
    /// Extension command responsible for opening the GitHub "new issue" page
    /// </summary>
    public class CreateIssueCommand : BaseCommand<CreateIssueCommand>
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(CreateIssueCommand));

        private readonly IAWSToolkitShellProvider _toolkitShell;
        private readonly ToolkitContext _toolkitContext;

        public CreateIssueCommand(IAWSToolkitShellProvider toolkitShell, ToolkitContext toolkitContext)
        {
            _toolkitShell = toolkitShell;
            _toolkitContext = toolkitContext;
        }

        public static Task<CreateIssueCommand> InitializeAsync(
            IAWSToolkitShellProvider toolkitShell,
            ToolkitContext toolkitContext,
            Guid menuGroup, int commandId,
            AsyncPackage package)
        {
            return InitializeAsync(
                () => new CreateIssueCommand(toolkitShell, toolkitContext),
                menuGroup, commandId,
                package);
        }

        protected override void Execute(object sender, EventArgs args)
        {
            try
            {
                _toolkitShell.OpenInBrowser(GitHubUrls.CreateNewIssueUrl, preferInternalBrowser: false);
                _toolkitContext.TelemetryLogger.RecordAwsReportPluginIssue(new AwsReportPluginIssue()
                {
                    AwsAccount = _toolkitContext.ConnectionManager.ActiveAccountId,
                    AwsRegion = _toolkitContext.ConnectionManager.ActiveRegion.Id,
                });
            }
            catch (Exception e)
            {
                Logger.Error($"Error launching issues page", e);
                _toolkitShell.ShowError("Failed to open the page", e.Message);
            }
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
