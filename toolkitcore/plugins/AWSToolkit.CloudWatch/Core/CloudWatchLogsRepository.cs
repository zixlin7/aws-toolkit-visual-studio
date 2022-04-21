using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.Util;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Util;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

using GetLogEventsRequest = Amazon.AWSToolkit.CloudWatch.Models.GetLogEventsRequest;
using LogGroup = Amazon.AWSToolkit.CloudWatch.Models.LogGroup;
using LogStream = Amazon.AWSToolkit.CloudWatch.Models.LogStream;
using OrderBy = Amazon.AWSToolkit.CloudWatch.Models.OrderBy;
using CloudWatchOrderBy = Amazon.CloudWatchLogs.OrderBy;

namespace Amazon.AWSToolkit.CloudWatch.Core
{
    public class CloudWatchLogsRepository : ICloudWatchLogsRepository
    {
        private readonly IAmazonCloudWatchLogs _client;

        public CloudWatchLogsRepository(AwsConnectionSettings connectionSettings,
            IAmazonCloudWatchLogs client)
        {
            ConnectionSettings = connectionSettings;
            _client = client;
        }
        public AwsConnectionSettings ConnectionSettings { get; }

        public async Task<PaginatedLogResponse<LogGroup>> GetLogGroupsAsync(GetLogGroupsRequest logGroupsRequest,
            CancellationToken cancelToken)
        {
            var request = CreateDescribeLogGroupsRequest(logGroupsRequest);
            var response = await _client.DescribeLogGroupsAsync(request, cancelToken);

            var logGroups = response.LogGroups.Select(logGroup => logGroup.ToLogGroup()).ToList();
            return new PaginatedLogResponse<LogGroup>(response.NextToken, logGroups);
        }

        public async Task<PaginatedLogResponse<LogStream>> GetLogStreamsAsync(
            GetLogStreamsRequest logStreamsRequest, CancellationToken cancelToken)
        {
            VerifyLogGroupIsValid(logStreamsRequest.LogGroup);

            var request = CreateDescribeLogStreamsRequest(logStreamsRequest);
            var response = await _client.DescribeLogStreamsAsync(request, cancelToken);

            var logStreams = response.LogStreams.Select(logStream => logStream.ToLogStream()).ToList();

            return new PaginatedLogResponse<LogStream>(response.NextToken, logStreams);
        }

        public async Task<PaginatedLogResponse<LogEvent>> GetLogEventsAsync(GetLogEventsRequest logEventsRequest,
            CancellationToken cancelToken)
        {
            VerifyLogGroupIsValid(logEventsRequest.LogGroup);
            VerifyLogStreamIsValid(logEventsRequest.LogStream);

            var request = CreateFilterLogEventsRequest(logEventsRequest);
            var response = await _client.FilterLogEventsAsync(request, cancelToken);

            var logEvents = response.Events.Select(logEvent => logEvent.ToLogEvent()).ToList();
            return new PaginatedLogResponse<LogEvent>(response.NextToken, logEvents);
        }

        public async Task<bool> DeleteLogGroupAsync(string logGroup,
            CancellationToken cancelToken)
        {
            VerifyLogGroupIsValid(logGroup);

            var request = new DeleteLogGroupRequest(logGroup);
            var response = await _client.DeleteLogGroupAsync(request, cancelToken);

            return response.HttpStatusCode == HttpStatusCode.OK;
        }

        private DescribeLogGroupsRequest CreateDescribeLogGroupsRequest(GetLogGroupsRequest logGroupsRequest)
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

            return request;
        }

        private DescribeLogStreamsRequest CreateDescribeLogStreamsRequest(GetLogStreamsRequest logStreamsRequest)
        {
            if (logStreamsRequest.OrderBy == OrderBy.LogStreamName)
            {
                return CreateLogStreamsRequestByName(logStreamsRequest);
            }

            return CreateLogStreamsRequestByTime(logStreamsRequest);
        }

        private DescribeLogStreamsRequest CreateLogStreamsRequestByName(GetLogStreamsRequest logStreamsRequest)
        {
            var request = new DescribeLogStreamsRequest
            {
                LogGroupName = logStreamsRequest.LogGroup,
                Descending = logStreamsRequest.IsDescending,
                OrderBy = CloudWatchOrderBy.LogStreamName
            };

            if (!string.IsNullOrEmpty(logStreamsRequest.NextToken))
            {
                request.NextToken = logStreamsRequest.NextToken;
            }

            if (!string.IsNullOrEmpty(logStreamsRequest.FilterText))
            {
                request.LogStreamNamePrefix = logStreamsRequest.FilterText;
            }
            return request;
        }

        private DescribeLogStreamsRequest CreateLogStreamsRequestByTime(GetLogStreamsRequest logStreamsRequest)
        {
            var request = new DescribeLogStreamsRequest
            {
                LogGroupName = logStreamsRequest.LogGroup,
                Descending = logStreamsRequest.IsDescending,
                OrderBy = CloudWatchOrderBy.LastEventTime
            };

            if (!string.IsNullOrEmpty(logStreamsRequest.NextToken))
            {
                request.NextToken = logStreamsRequest.NextToken;
            }

            return request;
        }

        private FilterLogEventsRequest CreateFilterLogEventsRequest(GetLogEventsRequest logEventsRequest)
        {
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

            return request;
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
