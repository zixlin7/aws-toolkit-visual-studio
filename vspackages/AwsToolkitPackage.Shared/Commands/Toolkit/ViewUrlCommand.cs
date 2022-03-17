using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using log4net;

using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Toolkit
{
    /// <summary>
    /// Extension command responsible for opening a URL
    /// </summary>
    public class ViewUrlCommand : BaseCommand<ViewUrlCommand>
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ViewUrlCommand));

        private readonly string _url;
        private readonly IAWSToolkitShellProvider _toolkitShell;
        private readonly ToolkitContext _toolkitContext;

        public ViewUrlCommand(string url, IAWSToolkitShellProvider toolkitShell, ToolkitContext toolkitContext)
        {
            _url = url;
            _toolkitShell = toolkitShell;
            _toolkitContext = toolkitContext;
        }

        public static Task<ViewUrlCommand> InitializeAsync(
            string url,
            IAWSToolkitShellProvider toolkitShell,
            ToolkitContext toolkitContext,
            Guid menuGroup, int commandId,
            AsyncPackage package)
        {
            return InitializeAsync(
                () => new ViewUrlCommand(url, toolkitShell, toolkitContext),
                menuGroup, commandId,
                package);
        }

        protected override void Execute(object sender, EventArgs args)
        {
            try
            {
                _toolkitShell.OpenInBrowser(_url, preferInternalBrowser: false);
                _toolkitContext.TelemetryLogger.RecordAwsOpenUrl(new AwsOpenUrl()
                {
                    AwsAccount = _toolkitContext.ConnectionManager.ActiveAccountId,
                    AwsRegion = MetadataValue.NotApplicable,
                    Url = _url,
                    Result = Result.Succeeded,
                });
            }
            catch (Exception e)
            {
                Logger.Error($"Error opening url ({_url})", e);
                _toolkitShell.ShowError($"Failed to open Url", e.Message);
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
