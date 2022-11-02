using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using log4net;
using log4net.Appender;

using Microsoft.VisualStudio.Shell;

using InvalidOperationException = System.InvalidOperationException;

namespace Amazon.AWSToolkit.VisualStudio.Commands.Toolkit
{
    /// <summary>
    /// Extension command responsible for viewing location of the toolkit logs
    /// </summary>
    public class ViewToolkitLogsCommand : BaseCommand<ViewToolkitLogsCommand>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ViewToolkitLogsCommand));

        private readonly ToolkitContext _toolkitContext;

        public ViewToolkitLogsCommand(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
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
            try
            {
                var appender = Logger.Logger.Repository
                    .GetAppenders().OfType<FileAppender>().FirstOrDefault(f => f.Name.StartsWith("VsToolkitRolling"));
                var logFile = appender?.File;

                if (string.IsNullOrWhiteSpace(logFile) || !File.Exists(logFile))
                {
                    throw new InvalidOperationException($"Invalid log file path: {logFile}");
                }

                Process.Start("explorer.exe", "/select, " + logFile);
                _toolkitContext.TelemetryLogger.RecordToolkitViewLogs(new ToolkitViewLogs()
                {
                    AwsAccount = MetadataValue.NotApplicable,
                    AwsRegion = MetadataValue.NotApplicable,
                });
            }
            catch (Exception e)
            {
                Logger.Error($"Error viewing toolkit logs", e);
                _toolkitContext.ToolkitHost.ShowError("Failed to view toolkit logs", e.Message);
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
