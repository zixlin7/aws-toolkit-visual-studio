using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Logs.Core;
using Amazon.AWSToolkit.CloudWatch.Logs.Models;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Util;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace Amazon.AWSToolkit.CloudWatch.Logs.Util
{
    /// <summary>
    /// Handles exporting/downloading a <see cref="LogStream"/> to a file specified
    /// while displaying it's progress in Task Status Center
    /// </summary>
    public class ExportStreamHandler
    {
        private readonly ToolkitContext _toolkitContext;
        private readonly ICloudWatchLogsRepository _repository;
        private readonly string _logStream;
        private readonly string _logGroup;
        private readonly string _fileName;
        private readonly Action<TaskStatus, long> _recordMetric;
        private long _charactersLogged;
        private DateTime _endTime;

        public ExportStreamHandler(string logStream, string logGroup, string fileName, ToolkitContext toolkitContext,
            ICloudWatchLogsRepository repository, Action<TaskStatus, long> recordMetric)
        {
            _logStream = logStream;
            _logGroup = logGroup;
            _fileName = fileName;
            _toolkitContext = toolkitContext;
            _repository = repository;
            _recordMetric = recordMetric;
        }


        public async Task RunAsync(ITaskStatusNotifier notifier)
        {
            var result = await ExportAsync(notifier);

            ApplyExportResultToUi(result);
            ApplyExportResultToMetrics(result);

            if (result.Status != TaskStatus.Success)
            {
                throw result.Exception;
            }
        }

        private async Task<ExportResult> ExportAsync(ITaskStatusNotifier notifier)
        {
            var exportResult = new ExportResult();
            try
            {
                _endTime = DateTime.Now;
                string previousToken = null;
                string nextToken = null;
                _charactersLogged = 0;
                do
                {
                    if (notifier.CancellationToken.IsCancellationRequested)
                    {
                        notifier.CancellationToken.ThrowIfCancellationRequested();
                    }

                    _toolkitContext.ToolkitHost.UpdateStatus("Downloading");

                    previousToken = nextToken;
                    var request = CreateGetRequest(nextToken);
                    var response = await _repository.GetLogEventsAsync(request, notifier.CancellationToken)
                        .ConfigureAwait(false);

                    WriteToFile(response.Values, _fileName);

                    nextToken = response.NextToken;
                    exportResult.Count += response.Values.Count();
                    notifier.ProgressText = $"Downloaded events: {exportResult.Count}";
                } while (!string.Equals(nextToken, previousToken));

                exportResult.Status = TaskStatus.Success;
            }
            catch (Exception ex)
            {
                exportResult.Status = ex is OperationCanceledException ? TaskStatus.Cancel : TaskStatus.Fail;
                exportResult.Exception = ex;
            }

            return exportResult;
        }


        private void ApplyExportResultToUi(ExportResult exportResult)
        {
            var statusMessage = CreateStatusMessage(exportResult);
            _toolkitContext.ToolkitHost.OutputToHostConsole(statusMessage, true);
            _toolkitContext.ToolkitHost.UpdateStatus($"Download Stream Status: {exportResult.Status}");
        }

        private string CreateStatusMessage(ExportResult exportResult)
        {
            var statusMsg = string.Empty;
            switch (exportResult.Status)
            {
                case TaskStatus.Success:
                    statusMsg = $"CloudWatch Logs downloaded. Stream: {_logStream}, File: {_fileName}";
                    break;
                case TaskStatus.Cancel:
                    statusMsg = $"CloudWatch Logs download cancelled. Stream: {_logStream}, File: {_fileName}, Events saved: {exportResult.Count}";
                    break;
                case TaskStatus.Fail:
                    var statusDetail = string.Empty;
                    if (exportResult.Count > 0)
                    {
                        statusDetail = $", File: {_fileName}, Events saved: {exportResult.Count}";
                    }

                    statusMsg = $"CloudWatch Logs download failed. Stream: {_logStream}{statusDetail}";
                    break;
            }

            return statusMsg;
        }

        private void WriteToFile(IEnumerable<LogEvent> logEvents, string fileName)
        {
            using (StreamWriter writer = File.AppendText(fileName))
            {
                logEvents.ToList().ForEach(x =>
                {
                    var msg = StringUtils.NormalizeLineEnding(x.Message);
                    writer.WriteLine(msg);
                    _charactersLogged += msg.Length;
                });
            }
        }

        private void ApplyExportResultToMetrics(ExportResult exportResult)
        {
            _recordMetric(exportResult.Status, _charactersLogged);
        }

        private GetLogEventsRequest CreateGetRequest(string nextToken)
        {
            var request = new GetLogEventsRequest
            {
                LogGroup = _logGroup, LogStream = _logStream, EndTime = _endTime
            };
            if (!string.IsNullOrWhiteSpace(nextToken))
            {
                request.NextToken = nextToken;
            }

            return request;
        }
    }
}
