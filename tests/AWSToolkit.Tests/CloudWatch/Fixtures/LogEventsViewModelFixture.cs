using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;

using Moq;

namespace AWSToolkit.Tests.CloudWatch.Fixtures
{
    public class LogEventsViewModelFixture : BaseLogsViewModelFixture
    {
        public LogGroup SampleLogGroup => SampleLogGroups.First();
        public LogStream SampleLogStream => SampleLogStreams.First();

        public LogEventsViewModelFixture() : base()
        {
            StubGetLogEventsToReturn(SampleToken, SampleLogEvents);
            StubFilterLogEventsToReturn(SampleToken, SampleLogEvents);
        }

        public LogEventsViewModel CreateViewModel()
        {
            var viewModel = new LogEventsViewModel(Repository.Object, ContextFixture.ToolkitContext)
            {
                LogGroup = SampleLogGroup.Name, LogStream = SampleLogStream.Name
            };
            return viewModel;
        }

        public void StubGetLogEventsToReturn(string nextToken, List<LogEvent> logEvents)
        {
            var response = new PaginatedLogResponse<LogEvent>(nextToken, logEvents);
            Repository.Setup(mock =>
                    mock.GetLogEventsAsync(It.IsAny<GetLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }

        public void StubFilterLogEventsToReturn(string nextToken, List<LogEvent> logEvents)
        {
            var response = new PaginatedLogResponse<LogEvent>(nextToken, logEvents);
            Repository.Setup(mock =>
                    mock.FilterLogEventsAsync(It.IsAny<FilterLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }
    }
}
