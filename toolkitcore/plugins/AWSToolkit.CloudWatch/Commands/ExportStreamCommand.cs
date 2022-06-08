using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Util;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Context;

using log4net;

using Microsoft.Win32;

namespace Amazon.AWSToolkit.CloudWatch.Commands
{
    public class ExportStreamCommand
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ExportStreamCommand));

        public static ICommand Create(ToolkitContext toolkitContext, ICloudWatchLogsRepository repository)
        {
            return new AsyncRelayCommand(parameter => DownloadStreamAsync(parameter, toolkitContext, repository));
        }

        private static async Task DownloadStreamAsync(object parameter, ToolkitContext toolkitContext,
            ICloudWatchLogsRepository repository)
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

                await DownloadStreamAsync(logStream, logGroup, toolkitContext, repository);
            }
            catch (Exception ex)
            {
                Logger.Error("Error downloading log stream", ex);
                toolkitContext.ToolkitHost.ShowError($"Error downloading log stream: {ex.Message}");
            }
        }

        private static async Task DownloadStreamAsync(string logStream, string logGroup, ToolkitContext toolkitContext,
            ICloudWatchLogsRepository repository)
        {
            var dlg = CreateSaveFileDialog(logStream);
            if (!dlg.ShowDialog().GetValueOrDefault())
            {
                return;
            }

            var taskStatusNotifier = await CreateTaskStatusNotifier(toolkitContext, logStream);

            var exportStreamHandler =
                new ExportStreamHandler(logStream, logGroup, dlg.FileName, toolkitContext, repository);

            taskStatusNotifier.ShowTaskStatus(exportStreamHandler.RunAsync);
        }

        private static SaveFileDialog CreateSaveFileDialog(string logStream)
        {
            var dlg = new SaveFileDialog
            {
                // Default file name
                FileName = logStream,
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
    }
}
