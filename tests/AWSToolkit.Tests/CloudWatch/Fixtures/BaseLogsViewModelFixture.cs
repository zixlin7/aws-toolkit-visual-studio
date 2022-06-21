using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

namespace AWSToolkit.Tests.CloudWatch.Fixtures
{
    public abstract class BaseLogsViewModelFixture
    {
        public readonly ToolkitContextFixture ContextFixture = new ToolkitContextFixture();
        public AwsConnectionSettings AwsConnectionSettings;
        public Mock<ICloudWatchLogsRepository> Repository { get; } = new Mock<ICloudWatchLogsRepository>();

        public List<LogGroup> SampleLogGroups { get; }
        public List<LogStream> SampleLogStreams { get; }
        public List<LogEvent> SampleLogEvents { get; }

        public string SampleToken => "sample-token";
        public Mock<IAWSToolkitShellProvider> ToolkitHost => ContextFixture.ToolkitHost;

        protected BaseLogsViewModelFixture()
        {
            AwsConnectionSettings = new AwsConnectionSettings(null, null);

            ContextFixture.SetupExecuteOnUIThread();
            SetupRepository();

            SampleLogGroups = CreateSampleLogGroups();
            SampleLogStreams = CreateSampleLogStreams();
            SampleLogEvents = CreateSampleLogEvents();
        }

        public List<LogGroup> CreateSampleLogGroups()
        {
            return Enumerable.Range(1, 3).Select(i =>
            {
                var guid = Guid.NewGuid().ToString();
                return new LogGroup() { Name = $"lg-{guid}", Arn = $"lg-{guid}-arn" };
            }).ToList();
        }

        public List<LogStream> CreateSampleLogStreams()
        {
            return Enumerable.Range(1, 3).Select(i =>
            {
                var guid = Guid.NewGuid().ToString();
                return new LogStream() { Name = $"lg-{guid}", Arn = $"lg-{guid}-arn", LastEventTime = DateTime.Now };
            }).ToList();
        }

        public List<LogEvent> CreateSampleLogEvents()
        {
            return Enumerable.Range(1, 3).Select(i =>
            {
                var guid = Guid.NewGuid().ToString();
                return new LogEvent() { Message = $"sample-message-{guid}", Timestamp = DateTime.Now };
            }).ToList();
        }

        public void SetupToolkitHostConfirm(bool result)
        {
            ToolkitHost.Setup(mock => mock.Confirm(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(result);
        }

        protected void SetupRepository()
        {
            Repository.SetupGet(m => m.ConnectionSettings).Returns(() => AwsConnectionSettings);
        }
    }
}
