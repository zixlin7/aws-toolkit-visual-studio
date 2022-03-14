using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.Util;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

using GetLogEventsRequest = Amazon.AWSToolkit.CloudWatch.Models.GetLogEventsRequest;
using LogGroup = Amazon.AWSToolkit.CloudWatch.Models.LogGroup;
using LogStream = Amazon.AWSToolkit.CloudWatch.Models.LogStream;

namespace Amazon.AWSToolkit.CloudWatch.Core
{
    public class CloudWatchLogsRepository : ICloudWatchLogsRepository
    {
        private readonly IAmazonCloudWatchLogs _client;

        public CloudWatchLogsRepository(IAmazonCloudWatchLogs client)
        {
            _client = client;
        }

        public async Task<Tuple<string, List<LogGroup>>> GetLogGroupsAsync(GetLogGroupsRequest logGroupsRequest,
            CancellationToken cancelToken)
        {
            var request = new DescribeLogGroupsRequest();

            if (!string.IsNullOrEmpty(logGroupsRequest.FilterText))
            {
                request.LogGroupNamePrefix = logGroupsRequest.FilterText;
            }

            if (!string.IsNullOrEmpty(logGroupsRequest.NextToken))
            {
                request.NextToken = logGroupsRequest.NextToken;
            }

            var response = await _client.DescribeLogGroupsAsync(request, cancelToken);
            var logGroups = response.LogGroups.Select(logGroup => logGroup.ToLogGroup()).ToList();
            return Tuple.Create(response.NextToken, logGroups);
        }

        public async Task<Tuple<string, List<LogStream>>> GetLogStreamsOrderByTimeAsync(
            GetLogStreamsRequest logStreamsRequest, CancellationToken cancelToken)
        {
            VerifyLogGroupIsValid(logStreamsRequest.LogGroup);

            var request = new DescribeLogStreamsRequest
            {
                LogGroupName = logStreamsRequest.LogGroup,
                Descending = true,
                OrderBy = OrderBy.LastEventTime
            };

            if (!string.IsNullOrEmpty(logStreamsRequest.NextToken))
            {
                request.NextToken = logStreamsRequest.NextToken;
            }

            var response = await _client.DescribeLogStreamsAsync(request, cancelToken);

            var logStreams = response.LogStreams.Select(logStream => logStream.ToLogStream()).ToList();
            return Tuple.Create(response.NextToken, logStreams);
        }

        public async Task<Tuple<string, List<LogStream>>> GetLogStreamsOrderByNameAsync(
            GetLogStreamsRequest logStreamsRequest, CancellationToken cancelToken)
        {
            VerifyLogGroupIsValid(logStreamsRequest.LogGroup);

            var request = new DescribeLogStreamsRequest
            {
                LogGroupName = logStreamsRequest.LogGroup,
                LogStreamNamePrefix = logStreamsRequest.FilterText,
                Descending = true,
                OrderBy = OrderBy.LogStreamName
            };

            if (!string.IsNullOrEmpty(logStreamsRequest.NextToken))
            {
                request.NextToken = logStreamsRequest.NextToken;
            }

            var response = await _client.DescribeLogStreamsAsync(request, cancelToken);

            var logStreams = response.LogStreams.Select(logStream => logStream.ToLogStream()).ToList();
            return Tuple.Create(response.NextToken, logStreams);
        }

        public async Task<Tuple<string, List<LogEvent>>> GetLogEventsAsync(GetLogEventsRequest logEventsRequest,
            CancellationToken cancelToken)
        {
            VerifyLogGroupIsValid(logEventsRequest.LogGroup);
            VerifyLogStreamIsValid(logEventsRequest.LogStream);

            var request = new FilterLogEventsRequest
            {
                LogGroupName = logEventsRequest.LogGroup,
                LogStreamNames = new List<string> { logEventsRequest.LogStream },
                FilterPattern = logEventsRequest.FilterText,
            };

            if (logEventsRequest.StartTime != default(DateTime))
            {
                request.StartTime = logEventsRequest.StartTime.AsUnixMilliseconds();
            }

            if (logEventsRequest.EndTime != default(DateTime))
            {
                request.EndTime = logEventsRequest.EndTime.AsUnixMilliseconds();
            }

            if (!string.IsNullOrEmpty(logEventsRequest.NextToken))
            {
                request.NextToken = logEventsRequest.NextToken;
            }

            var response = await _client.FilterLogEventsAsync(request, cancelToken);

            var logEvents = response.Events.Select(logEvent => logEvent.ToLogEvent()).ToList();
            return Tuple.Create(response.NextToken, logEvents);
        }

        public async Task<bool> DeleteLogGroupAsync(string logGroup,
            CancellationToken cancelToken)
        {
            VerifyLogGroupIsValid(logGroup);

            var request = new DeleteLogGroupRequest(logGroup);

            var response = await _client.DeleteLogGroupAsync(request, cancelToken);

            return response.HttpStatusCode == HttpStatusCode.OK;
        }

        private void VerifyLogGroupIsValid(string logGroup)
        {
            if (string.IsNullOrEmpty(logGroup))
            {
                throw new InvalidParameterException(
                    $"{logGroup} is an invalid log group name.");
            }
        }

        private void VerifyLogStreamIsValid(string logStream)
        {
            if (string.IsNullOrEmpty(logStream))
            {
                throw new InvalidParameterException(
                    $"{logStream} is an invalid log stream name.");
            }
        }
    }
}
