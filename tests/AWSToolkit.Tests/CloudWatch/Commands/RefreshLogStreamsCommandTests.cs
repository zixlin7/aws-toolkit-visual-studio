using System;
using System.Threading;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Commands;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using AWSToolkit.Tests.CloudWatch.Fixtures;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.Commands
{
    public class RefreshLogStreamsCommandTests
    {
        private readonly LogStreamsFixture _streamsFixture = new LogStreamsFixture();
        private readonly LogStreamsViewModel _viewModel;
        private readonly ICommand _command;

        public RefreshLogStreamsCommandTests()
        {
            _viewModel = _streamsFixture.CreateViewModel();
            _command = RefreshLogsCommand.Create(_viewModel);
        }

        [Fact]
        public void Execute()
        {
            _viewModel.NextToken = "initial-token";

            _command.Execute(null);

            Assert.Equal(_streamsFixture.SampleToken, _viewModel.NextToken);
            Assert.Equal(_streamsFixture.SampleLogStreams, _viewModel.LogStreams);
            Assert.Empty(_viewModel.ErrorMessage);
            _streamsFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsRefresh(CloudWatchResourceType.LogGroup);
        }

        [Fact]
        public void Execute_WhenError()
        {
            _streamsFixture.Repository.Setup(mock =>
                    mock.GetLogStreamsAsync(It.IsAny<GetLogStreamsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NullReferenceException("null reference found"));

            _command.Execute(null);

            var logType = _viewModel.GetLogTypeDisplayName();
            Assert.Contains($"Error refreshing {logType}", _viewModel.ErrorMessage);
            _streamsFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsRefresh(CloudWatchResourceType.LogGroup);
        }
    }
}
