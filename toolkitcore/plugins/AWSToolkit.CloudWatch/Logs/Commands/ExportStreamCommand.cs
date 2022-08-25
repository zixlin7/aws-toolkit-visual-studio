using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Logs.Core;
using Amazon.AWSToolkit.CloudWatch.Logs.Util;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Util;

using log4net;

using Microsoft.Win32;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace Amazon.AWSToolkit.CloudWatch.Logs.Commands
{
    public class ExportStreamCommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ExportStreamCommand));

        public static ICommand Create(ICloudWatchLogsRepository repository, ToolkitContext toolkitContext)
        {
            return new AsyncRelayCommand(parameter => ExecuteAsync(parameter, repository, toolkitContext));
        }

        private static async Task ExecuteAsync(object parameter,
            ICloudWatchLogsRepository repository, ToolkitContext toolkitContext)
        {
            void RecordDownloadMetric(TaskStatus executionResult, long charactersLogged)
            {
                RecordDownload(executionResult, charactersLogged, repository.ConnectionSettings, toolkitContext);
            }

            await DownloadStreamAsync(parameter, repository, toolkitContext, RecordDownloadMetric);
        }

        private static async Task DownloadStreamAsync(object parameter,
            ICloudWatchLogsRepository repository, ToolkitContext toolkitContext,
            Action<TaskStatus, long> recordMetric)
        {
            try
            {
                var parameters = (object[]) parameter;
                if (parameters == null || parameters.Count() != 2)
                {
                    throw new ArgumentException($"Expected parameters: 2, Found: {parameters?.Count()}");
                }

                if (parameters.Any(x => x.GetType() != typeof(string)))
                {
                    throw new ArgumentException($"Parameters are not of expected type: {typeof(string)}");
                }

                var logStream = parameters[1] as string;
                var logGroup = parameters[0] as string;

                await DownloadStreamAsync(logStream, logGroup, toolkitContext, repository, recordMetric);
            }
            catch (Exception ex)
            {
                Logger.Error("Error downloading log stream", ex);
                toolkitContext.ToolkitHost.ShowError($"Error downloading log stream: {ex.Message}");

                recordMetric(TaskStatus.Fail, 0);
            }
        }

        private static async Task DownloadStreamAsync(string logStream, string logGroup,
            ToolkitContext toolkitContext, ICloudWatchLogsRepository repository,
            Action<TaskStatus, long> recordMetric)
        {
            var dlg = CreateSaveFileDialog(logStream);
            if (!dlg.ShowDialog().GetValueOrDefault())
            {
                recordMetric(TaskStatus.Cancel, 0);
                return;
            }

            var taskStatusNotifier = await CreateTaskStatusNotifier(toolkitContext, logStream);

            var exportStreamHandler =
                new ExportStreamHandler(logStream, logGroup, dlg.FileName, toolkitContext, repository, recordMetric);

            taskStatusNotifier.ShowTaskStatus(exportStreamHandler.RunAsync);
        }

        private static SaveFileDialog CreateSaveFileDialog(string logStream)
        {
            var dlg = new SaveFileDialog
            {
                // Default file name
                FileName = StringUtils.SanitizeFilename(logStream),
                DefaultExt = ".txt",
                Title = "Export to a text file",
                Filter = "Text Files (*.txt) | *.txt | All Files (*.*) | *.*"
            };

            return dlg;
        }

        private static async Task<ITaskStatusNotifier> CreateTaskStatusNotifier(ToolkitContext toolkitContext,
            string logStream)
        {
            var taskStatus = await toolkitContext.ToolkitHost.CreateTaskStatusNotifier();

            taskStatus.Title = $"Downloading Log Stream: {logStream}";
            taskStatus.ProgressText = "Downloading...";
            taskStatus.CanCancel = true;
            return taskStatus;
        }

        private static void RecordDownload(TaskStatus exportResult,
            double charactersLogged,
            AwsConnectionSettings connectionSettings,
            ToolkitContext toolkitContext)
        {
            toolkitContext.TelemetryLogger.RecordCloudwatchlogsDownload(new CloudwatchlogsDownload()
            {
                AwsAccount = MetricsMetadata.AccountIdOrDefault(connectionSettings.GetAccountId(toolkitContext.ServiceClientManager)),
                AwsRegion = MetricsMetadata.RegionOrDefault(connectionSettings.Region),
                CloudWatchResourceType = CloudWatchResourceType.LogStream,
                Result = exportResult.AsMetricsResult(),
                Value = charactersLogged,
            });
        }
    }
}
