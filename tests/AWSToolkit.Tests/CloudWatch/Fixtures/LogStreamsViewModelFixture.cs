using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

namespace AWSToolkit.Tests.CloudWatch.Fixtures
{
    public class LogStreamsFixture
    {
        public readonly ToolkitContextFixture ContextFixture = new ToolkitContextFixture();
        public Mock<ICloudWatchLogsRepository> Repository { get; } = new Mock<ICloudWatchLogsRepository>();
        public List<LogStream> SampleLogStreams { get; }
        public string SampleToken => "sample-token";
        public LogGroup SampleLogGroup { get; } = new LogGroup() { Name = "lg", Arn = "lg-arn" };
        public AwsConnectionSettings AwsConnectionSettings;

        public LogStreamsFixture()
        {
            AwsConnectionSettings = new AwsConnectionSettings(null, null);

            ContextFixture.SetupExecuteOnUIThread();
            SampleLogStreams = CreateSampleLogStreams();
            StubGetLogStreamsToReturn(SampleToken, SampleLogStreams);
            SetupRepository();
        }

        public LogStreamsViewModel CreateViewModel()
        {
            var viewModel = new LogStreamsViewModel(Repository.Object, ContextFixture.ToolkitContext)
            {
                LogGroup = SampleLogGroup
            };
            return viewModel;
        }

        public List<LogStream> CreateSampleLogStreams()
        {
            return Enumerable.Range(1, 3).Select(i =>
            {
                var guid = Guid.NewGuid().ToString();
                return new LogStream() { Name = $"lg-{guid}", Arn = $"lg-{guid}-arn", LastEventTime = DateTime.Now };
            }).ToList();
        }

        public void StubGetLogStreamsToReturn(string nextToken, List<LogStream> logStreams)
        {
            var response = new PaginatedLogResponse<LogStream>(nextToken, logStreams);
            Repository.Setup(mock =>
                    mock.GetLogStreamsAsync(It.IsAny<GetLogStreamsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
        }

        private void SetupRepository()
        {
            Repository.SetupGet(m => m.ConnectionSettings).Returns(() => AwsConnectionSettings);
        }
    }
}
