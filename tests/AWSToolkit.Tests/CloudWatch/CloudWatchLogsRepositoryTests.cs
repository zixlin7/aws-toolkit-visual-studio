using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.Util;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

using Moq;

using Xunit;

using GetLogEventsRequest = Amazon.AWSToolkit.CloudWatch.Models.GetLogEventsRequest;
using LogGroup = Amazon.CloudWatchLogs.Model.LogGroup;
using LogStream = Amazon.CloudWatchLogs.Model.LogStream;
using OrderBy = Amazon.AWSToolkit.CloudWatch.Models.OrderBy;
using ToolkitLogEvent = Amazon.AWSToolkit.CloudWatch.Models.LogEvent;
using ToolkitLogGroup = Amazon.AWSToolkit.CloudWatch.Models.LogGroup;
using ToolkitLogStream = Amazon.AWSToolkit.CloudWatch.Models.LogStream;

namespace AWSToolkit.Tests.CloudWatch
{
    public class CloudWatchLogsRepositoryTests
    {
        private readonly Mock<IAmazonCloudWatchLogs> _cwlClient = new Mock<IAmazonCloudWatchLogs>();
        private readonly CancellationToken _cancelToken = new CancellationToken();
        private readonly GetLogGroupsRequest _sampleLogGroupRequest = new GetLogGroupsRequest();
        private readonly GetLogStreamsRequest _sampleLogStreamsRequest = new GetLogStreamsRequest();
        private readonly GetLogEventsRequest _sampleLogEventsRequest = new GetLogEventsRequest();
        private readonly string _nextToken = "sample-token";
        private readonly string _sampleLogGroup = "sample-log-group";
        private readonly string _sampleLogStream = "sample-log-stream";

        private readonly CloudWatchLogsRepository _repository;

        public CloudWatchLogsRepositoryTests()
        {
            _repository = new CloudWatchLogsRepository(_cwlClient.Object);
        }

        [Fact]
        public async Task GetLogGroupsAsync_ThrowsNull()
        {
            SetupDescribeLogGroups(null);

            await Assert.ThrowsAsync<NullReferenceException>(async () =>
                await _repository.GetLogGroupsAsync(_sampleLogGroupRequest, _cancelToken));
        }

        [Fact]
        public async Task GetLogGroupsAsync()
        {
            var sampleSdkLogGroups = CreateSampleSdkLogGroups();
            var output = new DescribeLogGroupsResponse() { NextToken = _nextToken, LogGroups = sampleSdkLogGroups };
            SetupDescribeLogGroups(output);

            var response = await _repository.GetLogGroupsAsync(_sampleLogGroupRequest, _cancelToken);
            var expectedLogGroups = CreateSampleLogGroups();

            Assert.Equal(_nextToken, response.NextToken);
            Assert.Equal(3, response.Values.Count());
            Assert.Equal(expectedLogGroups, response.Values);
        }

        [Fact]
        public async Task GetLogStreamsAsync_ThrowsNull()
        {
            _sampleLogStreamsRequest.LogGroup = _sampleLogGroup;
            SetupDescribeLogStreams((DescribeLogStreamsResponse) null);

            await Assert.ThrowsAsync<NullReferenceException>(async () =>
                await _repository.GetLogStreamsAsync(_sampleLogStreamsRequest, OrderBy.LogStreamName, _cancelToken));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task GetLogStreamsAsync_InvalidLogGroup(string logGroup)
        {
            _sampleLogStreamsRequest.LogGroup = logGroup;
            await Assert.ThrowsAsync<InvalidParameterException>(async () =>
                await _repository.GetLogStreamsAsync(_sampleLogStreamsRequest, OrderBy.LogStreamName, _cancelToken));
        }


        [Fact]
        public async Task GetLogStreamsAsync()
        {
            _sampleLogStreamsRequest.LogGroup = _sampleLogGroup;
            var sampleSdkLogStreams = CreateSampleSdkLogStreams();
            var output = new DescribeLogStreamsResponse() { NextToken = _nextToken, LogStreams = sampleSdkLogStreams };
            SetupDescribeLogStreams(output);

            var response = await _repository.GetLogStreamsAsync(_sampleLogStreamsRequest, OrderBy.LastEventTime, _cancelToken);
            var expectedLogStreams = CreateSampleLogStreams();

            Assert.Equal(_nextToken, response.NextToken);
            Assert.Equal(3, response.Values.Count());
            Assert.Equal(expectedLogStreams, response.Values);
        }

        [Fact]
        public async Task GetLogEventsAsync_ThrowsNull()
        {
            _sampleLogEventsRequest.LogGroup = _sampleLogGroup;
            _sampleLogEventsRequest.LogStream = _sampleLogStream;
            SetupFilterLogEvents((FilterLogEventsResponse) null);

            await Assert.ThrowsAsync<NullReferenceException>(async () =>
                await _repository.GetLogEventsAsync(_sampleLogEventsRequest, _cancelToken));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task GetLogEventsAsync_InvalidLogGroup(string logGroup)
        {
            _sampleLogEventsRequest.LogGroup = logGroup;
            var exception = await Assert.ThrowsAsync<InvalidParameterException>(async () =>
                await _repository.GetLogEventsAsync(_sampleLogEventsRequest, _cancelToken));
            Assert.Contains("is an invalid log group name.", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task GetLogEventsAsync_InvalidLogStream(string logStream)
        {
            _sampleLogEventsRequest.LogGroup = _sampleLogGroup;
            _sampleLogEventsRequest.LogStream = logStream;
            var exception = await Assert.ThrowsAsync<InvalidParameterException>(async () =>
                await _repository.GetLogEventsAsync(_sampleLogEventsRequest, _cancelToken));
            Assert.Contains("is an invalid log stream name.", exception.Message);
        }

        [Fact]
        public async Task GetLogEventsAsync()
        {
            _sampleLogEventsRequest.LogGroup = _sampleLogGroup;
            _sampleLogEventsRequest.LogStream = _sampleLogStream;

            var sampleSdkLogEvents = CreateSampleSdkLogEvents();
            var output = new FilterLogEventsResponse() { NextToken = _nextToken, Events = sampleSdkLogEvents };
            SetupFilterLogEvents(output);

            var response = await _repository.GetLogEventsAsync(_sampleLogEventsRequest, _cancelToken);
            var expectedLogEvents = CreateSampleLogEvents();

            Assert.Equal(_nextToken, response.NextToken);
            Assert.Equal(3, response.Values.Count());
            Assert.Equal(expectedLogEvents, response.Values);
        }

        [Fact]
        public async Task DeleteLogGroupAsync_ThrowsNull()
        {
            SetupDeleteLogGroup(null);

            await Assert.ThrowsAsync<NullReferenceException>(async () =>
                await _repository.DeleteLogGroupAsync(_sampleLogGroup, _cancelToken));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task DeleteLogGroupAsync_InvalidLogGroup(string logGroup)
        {
            await Assert.ThrowsAsync<InvalidParameterException>(async () =>
                await _repository.DeleteLogGroupAsync(logGroup, _cancelToken));
        }

        [Fact]
        public async Task DeleteLogGroupsAsync_Fails()
        {
            var output = new DeleteLogGroupResponse { HttpStatusCode = HttpStatusCode.BadRequest };
            SetupDeleteLogGroup(output);

            var response = await _repository.DeleteLogGroupAsync(_sampleLogGroup, _cancelToken);

            Assert.False(response);
        }

        [Fact]
        public async Task DeleteLogGroupsAsync()
        {
            var output = new DeleteLogGroupResponse { HttpStatusCode = HttpStatusCode.OK };
            SetupDeleteLogGroup(output);

            var response = await _repository.DeleteLogGroupAsync(_sampleLogGroup, _cancelToken);

            Assert.True(response);
        }

        private void SetupDescribeLogGroups(DescribeLogGroupsResponse response)
        {
            _cwlClient.Setup(mock => mock.DescribeLogGroupsAsync(It.IsAny<DescribeLogGroupsRequest>(), _cancelToken))
                .ReturnsAsync(response);
        }

        private void SetupDescribeLogStreams(DescribeLogStreamsResponse response)
        {
            _cwlClient.Setup(mock => mock.DescribeLogStreamsAsync(It.IsAny<DescribeLogStreamsRequest>(), _cancelToken))
                .ReturnsAsync(response);
        }

        private void SetupFilterLogEvents(FilterLogEventsResponse response)
        {
            _cwlClient.Setup(mock => mock.FilterLogEventsAsync(It.IsAny<FilterLogEventsRequest>(), _cancelToken))
                .ReturnsAsync(response);
        }

        private void SetupDeleteLogGroup(DeleteLogGroupResponse response)
        {
            _cwlClient.Setup(mock => mock.DeleteLogGroupAsync(It.IsAny<DeleteLogGroupRequest>(), _cancelToken))
                .ReturnsAsync(response);
        }

        private List<LogGroup> CreateSampleSdkLogGroups()
        {
            return Enumerable.Range(1, 3).Select(i => new LogGroup() { LogGroupName = $"lg-{i}", Arn = $"lg-{i}-arn" })
                .ToList();
        }

        private List<ToolkitLogGroup> CreateSampleLogGroups()
        {
            var sdkLogGroups = CreateSampleSdkLogGroups();
            return sdkLogGroups.Select(x => x.ToLogGroup()).ToList();
        }

        private static List<LogStream> CreateSampleSdkLogStreams()
        {
            return Enumerable.Range(1, 3).Select(i => new LogStream()
            {
                LogStreamName = $"ls-{i}", Arn = $"ls-{i}-arn", LastEventTimestamp = new DateTime(2022, 01, 01)
            }).ToList();
        }

        private static List<ToolkitLogStream> CreateSampleLogStreams()
        {
            var sdkLogStreams = CreateSampleSdkLogStreams();
            return sdkLogStreams.Select(x => x.ToLogStream()).ToList();
        }

        private static List<FilteredLogEvent> CreateSampleSdkLogEvents()
        {
            return Enumerable.Range(1, 3).Select(i => new FilteredLogEvent()
            {
                Message = $"le-message-{i}", Timestamp = 1641024000000
            }).ToList();
        }

        private static List<ToolkitLogEvent> CreateSampleLogEvents()
        {
            var sdkLogEvents = CreateSampleSdkLogEvents();
            return sdkLogEvents.Select(x => x.ToLogEvent()).ToList();
        }
    }
}
