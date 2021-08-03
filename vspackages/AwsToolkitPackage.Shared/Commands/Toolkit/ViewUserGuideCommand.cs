using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Shared;

using log4net;

using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Toolkit
{
    /// <summary>
    /// Extension command responsible for opening the AWS Toolkit User Guide
    /// </summary>
    public class ViewUserGuideCommand : BaseCommand<ViewUserGuideCommand>
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ViewUserGuideCommand));

        public const string UserGuideUrl = "https://docs.aws.amazon.com/toolkit-for-visual-studio/latest/user-guide/welcome.html";

        private readonly IAWSToolkitShellProvider _toolkitShell;
        private readonly ToolkitContext _toolkitContext;

        public ViewUserGuideCommand(IAWSToolkitShellProvider toolkitShell, ToolkitContext toolkitContext)
        {
            _toolkitShell = toolkitShell;
            _toolkitContext = toolkitContext;
        }

        public static Task<ViewUserGuideCommand> InitializeAsync(
            IAWSToolkitShellProvider toolkitShell,
            ToolkitContext toolkitContext,
            Guid menuGroup, int commandId,
            AsyncPackage package)
        {
            return InitializeAsync(
                () => new ViewUserGuideCommand(toolkitShell, toolkitContext),
                menuGroup, commandId,
                package);
        }

        protected override void Execute(object sender, EventArgs args)
        {
            try
            {
                _toolkitShell.OpenInBrowser(UserGuideUrl, preferInternalBrowser: false);
                _toolkitContext.TelemetryLogger.RecordAwsHelp(new AwsHelp()
                {
                    AwsAccount = _toolkitContext.ConnectionManager.ActiveAccountId,
                    AwsRegion = _toolkitContext.ConnectionManager.ActiveRegion.Id,
                });
            }
            catch (Exception e)
            {
                Logger.Error($"Error opening User Guide", e);
                _toolkitShell.ShowError("Failed to open User Guide", e.Message);
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
