using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

namespace AWSToolkit.Tests.CloudWatch.Fixtures
{
    public class LogGroupsViewModelFixture
    {
        public readonly ToolkitContextFixture ContextFixture = new ToolkitContextFixture();
        public Mock<ICloudWatchLogsRepository> Repository { get; } = new Mock<ICloudWatchLogsRepository>();
        public List<LogGroup> SampleLogGroups { get; }
        public string SampleToken => "sample-token";
        public Mock<IAWSToolkitShellProvider> ToolkitHost => ContextFixture.ToolkitHost;
        public AwsConnectionSettings AwsConnectionSettings;

        public LogGroupsViewModelFixture()
        {
            AwsConnectionSettings = new AwsConnectionSettings(null, null);

            ContextFixture.SetupExecuteOnUIThread();
            SampleLogGroups = CreateSampleLogGroups();
            StubGetLogGroupsToReturn(SampleToken, SampleLogGroups);
            SetupRepository();
        }

        public LogGroupsViewModel CreateViewModel()
        {
            return new LogGroupsViewModel(Repository.Object, ContextFixture.ToolkitContext);
        }

        public List<LogGroup> CreateSampleLogGroups()
        {
            return Enumerable.Range(1, 3).Select(i =>
            {
                var guid = Guid.NewGuid().ToString();
                return new LogGroup() { Name = $"lg-{guid}", Arn = $"lg-{guid}-arn" };
            }).ToList();
        }

        public void StubGetLogGroupsToReturn(string nextToken, List<LogGroup> logGroups)
        {
            var response = new PaginatedLogResponse<LogGroup>(nextToken, logGroups);
            Repository.Setup(mock =>
                    mock.GetLogGroupsAsync(It.IsAny<GetLogGroupsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }

        public void SetupToolkitHostConfirm(bool result)
        {
            ToolkitHost.Setup(mock => mock.Confirm(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(result);
        }

        private void SetupRepository()
        {
            Repository.SetupGet(m => m.ConnectionSettings).Returns(() => AwsConnectionSettings);
        }
    }
}
