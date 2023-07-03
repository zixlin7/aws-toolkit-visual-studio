using System;
using System.IO;
using System.Linq;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;

using log4net;
using log4net.Appender;

namespace Amazon.AWSToolkit.Commands
{
    public class OpenToolkitLogsCommand : Command
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(OpenToolkitLogsCommand));

        private readonly ToolkitContext _toolkitContext;

        public OpenToolkitLogsCommand(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        protected override void ExecuteCore(object parameter)
        {
            try
            {
                var logFile = _logger.Logger.Repository.GetAppenders().OfType<FileAppender>()
                    .FirstOrDefault(f => f.Name.StartsWith("VsToolkitRolling"))?.File;

                if (string.IsNullOrWhiteSpace(logFile) || !File.Exists(logFile))
                {
                    throw new InvalidOperationException($"Invalid log file path: {logFile}");
                }

                _toolkitContext.ToolkitHost.OpenInWindowsExplorer(logFile);
                _toolkitContext.TelemetryLogger.RecordToolkitViewLogs(new ToolkitViewLogs()
                {
                    AwsAccount = MetadataValue.NotApplicable,
                    AwsRegion = MetadataValue.NotApplicable,
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Error viewing toolkit logs", ex);
                _toolkitContext.ToolkitHost.ShowError("Failed to view toolkit logs", ex.Message);
            }
        }
    }
}
