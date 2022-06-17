using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.Util;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.IO;

using Moq;

using Xunit;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace AWSToolkit.Tests.CloudWatch.Util
{
    public class ExportStreamHandlerTests : IDisposable
    {
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();

        private readonly Mock<ITaskStatusNotifier> _taskNotifier = new Mock<ITaskStatusNotifier>();
        private readonly Mock<ICloudWatchLogsRepository> _repository = new Mock<ICloudWatchLogsRepository>();

        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly string _sampleLogGroup = "sample-log-group";
        private readonly string _sampleLogStream = "sample-log-stream";
        private string _sampleFileName;

        private List<LogEvent> _sampleLogEvents;
        private readonly ExportStreamHandler _handler;
        private TaskStatus _metricExportResult;
        private long _metricCharactersLogged;

        public ExportStreamHandlerTests()
        {
            Setup();
            _handler = new ExportStreamHandler(_sampleLogStream, _sampleLogGroup, _sampleFileName,
                _contextFixture.ToolkitContext, _repository.Object, OnRecordExportMetric);
        }

        private void OnRecordExportMetric(TaskStatus exportResult, long charactersLogged)
        {
            _metricExportResult = exportResult;
            _metricCharactersLogged = charactersLogged;
        }

        [Fact]
        public async Task RunAsync()
        {
            await _handler.RunAsync(_taskNotifier.Object);

            Assert.True(File.Exists(_sampleFileName));
            Assert.Equal(TaskStatus.Success, _metricExportResult);
            Assert.True(_metricCharactersLogged > 0);

            VerifyToolkitHostDownloadReports("CloudWatch Logs downloaded", "Success");
        }


        [Fact]
        public async Task RunAsync_Cancelled()
        {
            _tokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await _handler.RunAsync(_taskNotifier.Object);
            });

            Assert.False(File.Exists(_sampleFileName));
            Assert.Equal(TaskStatus.Cancel, _metricExportResult);
            Assert.Equal(0, _metricCharactersLogged);

            VerifyToolkitHostDownloadReports("CloudWatch Logs download cancelled", "Cancel");
        }


        [Fact]
        public async Task RunAsync_ExceptionThrown()
        {
            _repository.Setup(mock =>
                    mock.GetLogEventsAsync(It.IsAny<GetLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NullReferenceException());

            await Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await _handler.RunAsync(_taskNotifier.Object);
            });

            Assert.False(File.Exists(_sampleFileName));
            Assert.Equal(TaskStatus.Fail, _metricExportResult);
            Assert.Equal(0, _metricCharactersLogged);

            _contextFixture.ToolkitHost.Verify(
                mock => mock.UpdateStatus(It.Is<string>(s => s.Contains("Fail"))),
                Times.Once);
        }

        private void Setup()
        {
            _sampleLogEvents = CreateSampleLogEvents();
            _taskNotifier.Setup(x => x.CancellationToken).Returns(_tokenSource.Token);
            _sampleFileName = Path.Combine(_testLocation.TestFolder, _sampleLogStream);
            StubGetLogEventsToReturn(null, _sampleLogEvents);
        }

        private List<LogEvent> CreateSampleLogEvents()
        {
            return Enumerable.Range(1, 3).Select(i =>
            {
                var guid = Guid.NewGuid().ToString();
                return new LogEvent() { Message = $"sample-message-{guid}", Timestamp = DateTime.Now };
            }).ToList();
        }

        private void VerifyToolkitHostDownloadReports(string outputMessage, string statusMessage)
        {
            _contextFixture.ToolkitHost.Verify(
                mock => mock.OutputToHostConsole(It.Is<string>(s => s.Contains(outputMessage)), true), Times.Once);
            _contextFixture.ToolkitHost.Verify(mock => mock.UpdateStatus(It.Is<string>(s => s.Contains(statusMessage))),
                Times.Once);
        }

        private void StubGetLogEventsToReturn(string nextToken, List<LogEvent> logEvents)
        {
            var response = new PaginatedLogResponse<LogEvent>(nextToken, logEvents);
            _repository.Setup(mock =>
                    mock.GetLogEventsAsync(It.IsAny<GetLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }

        public void Dispose()
        {
            _testLocation?.Dispose();
        }
    }
}
