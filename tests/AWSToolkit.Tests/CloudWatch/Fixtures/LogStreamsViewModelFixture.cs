using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;

using Moq;

namespace AWSToolkit.Tests.CloudWatch.Fixtures
{
    public class LogStreamsFixture : BaseLogsViewModelFixture
    {
        public LogGroup SampleLogGroup => SampleLogGroups.First();

        public LogStreamsFixture() : base()
        {
            StubGetLogStreamsToReturn(SampleToken, SampleLogStreams);
        }

        public LogStreamsViewModel CreateViewModel()
        {
            var viewModel = new LogStreamsViewModel(Repository.Object, ContextFixture.ToolkitContext)
            {
                LogGroup = SampleLogGroup
            };
            return viewModel;
        }

        public void StubGetLogStreamsToReturn(string nextToken, List<LogStream> logStreams)
        {
            var response = new PaginatedLogResponse<LogStream>(nextToken, logStreams);
            Repository.Setup(mock =>
                    mock.GetLogStreamsAsync(It.IsAny<GetLogStreamsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }
    }
}
