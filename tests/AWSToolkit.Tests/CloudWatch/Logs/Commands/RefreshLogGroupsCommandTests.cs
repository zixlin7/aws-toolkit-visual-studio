using System;
using System.Threading;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Logs.Commands;
using Amazon.AWSToolkit.CloudWatch.Logs.Models;
using Amazon.AWSToolkit.CloudWatch.Logs.ViewModels;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using AWSToolkit.Tests.CloudWatch.Logs.Fixtures;
using AWSToolkit.Tests.CloudWatch.Logs.Util;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.Logs.Commands
{
    public class RefreshLogGroupsCommandTests
    {
        private readonly LogGroupsViewModel _viewModel;
        private readonly LogGroupsViewModelFixture _groupsFixture = new LogGroupsViewModelFixture();
        private readonly ICommand _command;

        public RefreshLogGroupsCommandTests()
        {
            _viewModel = _groupsFixture.CreateViewModel();
            _command = RefreshLogsCommand.Create(_viewModel);
        }

        [Fact]
        public void Execute()
        {
            _viewModel.NextToken = "initial-token";

            _command.Execute(null);

            Assert.Equal(_groupsFixture.SampleToken, _viewModel.NextToken);
            Assert.Equal(_groupsFixture.SampleLogGroups, _viewModel.LogGroups);
            Assert.Empty(_viewModel.ErrorMessage);
            _groupsFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsRefresh(CloudWatchResourceType.LogGroupList);
        }

        [Fact]
        public void Execute_WhenError()
        {
            _groupsFixture.Repository.Setup(mock =>
                    mock.GetLogGroupsAsync(It.IsAny<GetLogGroupsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NullReferenceException("null reference found"));

            _command.Execute(null);

            var logType = _viewModel.GetLogTypeDisplayName();
            Assert.Contains($"Error refreshing {logType}", _viewModel.ErrorMessage);
            _groupsFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsRefresh(CloudWatchResourceType.LogGroupList);
        }
    }
}
