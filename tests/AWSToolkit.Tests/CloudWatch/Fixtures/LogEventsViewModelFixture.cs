using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

namespace AWSToolkit.Tests.CloudWatch.Fixtures
{
    public class LogEventsViewModelFixture
    {
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        public Mock<ICloudWatchLogsRepository> Repository { get; } = new Mock<ICloudWatchLogsRepository>();
        public List<LogEvent> SampleLogEvents { get; }
        public string SampleToken => "sample-token";
        public LogGroup SampleLogGroup { get; } = new LogGroup() { Name = "lg", Arn = "lg-arn" };
        public LogStream SampleLogStream { get; } = new LogStream() { Name = "ls", Arn = "ls-arn" };

        public LogEventsViewModelFixture()
        {
            _contextFixture.SetupExecuteOnUIThread();
            SampleLogEvents = CreateSampleLogEvents();
            StubGetLogEventsToReturn(SampleToken, SampleLogEvents);
        }

        public LogEventsViewModel CreateViewModel()
        {
            var viewModel = new LogEventsViewModel(Repository.Object, _contextFixture.ToolkitContext)
            {
                LogGroup = SampleLogGroup, LogStream = SampleLogStream
            };
            return viewModel;
        }

        public List<LogEvent> CreateSampleLogEvents()
        {
            return Enumerable.Range(1, 3).Select(i =>
            {
                var guid = Guid.NewGuid().ToString();
                return new LogEvent() { Message = $"sample-message-{guid}", Timestamp = DateTime.Now };
            }).ToList();
        }

        public void StubGetLogEventsToReturn(string nextToken, List<LogEvent> logEvents)
        {
            var response = new PaginatedLogResponse<LogEvent>(nextToken, logEvents);
            Repository.Setup(mock =>
                    mock.GetLogEventsAsync(It.IsAny<GetLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }
    }
}
