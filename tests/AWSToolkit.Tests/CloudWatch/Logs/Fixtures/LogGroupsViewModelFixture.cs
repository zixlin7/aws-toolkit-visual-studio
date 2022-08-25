using System.Collections.Generic;
using System.Threading;

using Amazon.AWSToolkit.CloudWatch.Logs.Models;
using Amazon.AWSToolkit.CloudWatch.Logs.ViewModels;

using Moq;

namespace AWSToolkit.Tests.CloudWatch.Logs.Fixtures
{
    public class LogGroupsViewModelFixture : BaseLogsViewModelFixture
    {
        public LogGroupsViewModelFixture() : base()
        {
            StubGetLogGroupsToReturn(SampleToken, SampleLogGroups);
        }

        public LogGroupsViewModel CreateViewModel()
        {
            return new LogGroupsViewModel(Repository.Object, ContextFixture.ToolkitContext);
        }

        public void StubGetLogGroupsToReturn(string nextToken, List<LogGroup> logGroups)
        {
            var response = new PaginatedLogResponse<LogGroup>(nextToken, logGroups);
            Repository.Setup(mock =>
                    mock.GetLogGroupsAsync(It.IsAny<GetLogGroupsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }
    }
}
