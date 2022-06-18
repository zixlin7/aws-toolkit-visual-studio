﻿using System;
using System.Threading;
using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CloudWatch.Commands;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;

using AWSToolkit.Tests.CloudWatch.Fixtures;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.Commands
{
    public class RefreshLogEventsCommandTests
    {
        private readonly LogEventsViewModelFixture _eventsFixture = new LogEventsViewModelFixture();
        private readonly LogEventsViewModel _viewModel;
        private readonly ICommand _command;

        public RefreshLogEventsCommandTests()
        {
            _viewModel = _eventsFixture.CreateViewModel();
            _command = RefreshLogsCommand.Create(_viewModel);
        }

        [Fact]
        public void Execute()
        {
            _viewModel.NextToken = "initial-token";

            _command.Execute(null);

            Assert.Equal(_eventsFixture.SampleToken, _viewModel.NextToken);
            Assert.Equal(_eventsFixture.SampleLogEvents, _viewModel.LogEvents);
            Assert.Empty(_viewModel.ErrorMessage);
            _eventsFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsRefresh(CloudWatchResourceType.LogStream);
        }

        [Fact]
        public void Execute_WhenError()
        {
            _eventsFixture.Repository.Setup(mock =>
                    mock.GetLogEventsAsync(It.IsAny<GetLogEventsRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NullReferenceException("null reference found"));

            _command.Execute(null);

            var logType = _viewModel.GetLogTypeDisplayName();
            Assert.Contains($"Error refreshing {logType}", _viewModel.ErrorMessage);
            _eventsFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsRefresh(CloudWatchResourceType.LogStream);
        }
    }
}
