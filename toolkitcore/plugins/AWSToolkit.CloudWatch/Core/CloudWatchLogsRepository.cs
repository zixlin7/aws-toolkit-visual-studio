using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.Util;
using Amazon.CloudWatchLogs.Model;

using LogGroup = Amazon.AWSToolkit.CloudWatch.Models.LogGroup;
using LogStream = Amazon.AWSToolkit.CloudWatch.Models.LogStream;

namespace Amazon.AWSToolkit.CloudWatch.Core
{
    public class CloudWatchLogsRepository : ICloudWatchLogsRepository
    {
        public async Task<Tuple<string, List<LogGroup>>> GetLogGroupsAsync(CloudWatchLogsProperties logsProperties,
            CancellationToken cancelToken)
        {
            var client = logsProperties.CloudWatchLogsClient;

            var request = new DescribeLogGroupsRequest();

            if (!string.IsNullOrEmpty(logsProperties.LogGroup))
            {
                request.LogGroupNamePrefix = logsProperties.LogGroup;
            }

            if (!string.IsNullOrEmpty(logsProperties.NextToken))
            {
                request.NextToken = logsProperties.NextToken;
            }

            var response = await client.DescribeLogGroupsAsync(request, cancelToken);
            var logGroups = response.LogGroups.Select(logGroup => new LogGroup(logGroup)).ToList();
            return Tuple.Create(response.NextToken, logGroups);
        }

        public async Task<Tuple<string, List<LogStream>>> GetLogStreamsOrderByTimeAsync(
            CloudWatchLogsProperties logsProperties, CancellationToken cancelToken)
        {
            VerifyLogGroupIsValid(logsProperties.LogGroup);

            var client = logsProperties.CloudWatchLogsClient;

            var request = new DescribeLogStreamsRequest
            {
                LogGroupName = logsProperties.LogGroup,
                Descending = true,
                OrderBy = CloudWatchLogs.OrderBy.LastEventTime
            };

            if (!string.IsNullOrEmpty(logsProperties.NextToken))
            {
                request.NextToken = logsProperties.NextToken;
            }

            var response = await client.DescribeLogStreamsAsync(request, cancelToken);

            var logStreams = response.LogStreams.Select(logStream => new LogStream(logStream)).ToList();
            return Tuple.Create(response.NextToken, logStreams);
        }

        public async Task<Tuple<string, List<LogStream>>> GetLogStreamsOrderByNameAsync(
            CloudWatchLogsProperties logsProperties, CancellationToken cancelToken)
        {
            VerifyLogGroupIsValid(logsProperties.LogGroup);

            var client = logsProperties.CloudWatchLogsClient;

            var request = new DescribeLogStreamsRequest
            {
                LogGroupName = logsProperties.LogGroup,
                LogStreamNamePrefix = logsProperties.FilterText,
                Descending = true,
                OrderBy = CloudWatchLogs.OrderBy.LogStreamName
            };

            if (!string.IsNullOrEmpty(logsProperties.NextToken))
            {
                request.NextToken = logsProperties.NextToken;
            }

            var response = await client.DescribeLogStreamsAsync(request, cancelToken);

            var logStreams = response.LogStreams.Select(logStream => new LogStream(logStream)).ToList();
            return Tuple.Create(response.NextToken, logStreams);
        }

        public async Task<Tuple<string, List<LogEvent>>> GetLogEventsAsync(CloudWatchLogsProperties logsProperties,
            CancellationToken cancelToken)
        {
            VerifyLogGroupIsValid(logsProperties.LogGroup);
            VerifyLogStreamIsValid(logsProperties.LogStream);

            var client = logsProperties.CloudWatchLogsClient;

            var request = new FilterLogEventsRequest
            {
                LogGroupName = logsProperties.LogGroup,
                LogStreamNames = new List<string> { logsProperties.LogStream },
                FilterPattern = logsProperties.FilterText,
            };

            if (logsProperties.StartTime != default(DateTime))
            {
                request.StartTime = DateTimeUtil.ConvertLocalToUnixMilliseconds(logsProperties.StartTime);
            }

            if (logsProperties.EndTime != default(DateTime))
            {
                request.EndTime = DateTimeUtil.ConvertLocalToUnixMilliseconds(logsProperties.EndTime);
            }

            if (!string.IsNullOrEmpty(logsProperties.NextToken))
            {
                request.NextToken = logsProperties.NextToken;
            }

            var response = await client.FilterLogEventsAsync(request, cancelToken);

            var logEvents = response.Events.Select(logEvent => new LogEvent(logEvent)).ToList();
            return Tuple.Create(response.NextToken, logEvents);
        }

        public async Task<bool> DeleteLogGroupAsync(CloudWatchLogsProperties logsProperties,
            CancellationToken cancelToken)
        {
            VerifyLogGroupIsValid(logsProperties.LogGroup);

            var client = logsProperties.CloudWatchLogsClient;
            var request = new DeleteLogGroupRequest(logsProperties.LogGroup);

            var response = await client.DeleteLogGroupAsync(request, cancelToken);

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
