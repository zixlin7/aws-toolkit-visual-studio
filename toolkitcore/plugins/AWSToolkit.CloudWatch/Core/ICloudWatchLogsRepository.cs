using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Models;

namespace Amazon.AWSToolkit.CloudWatch.Core
{
    /// <summary>
    /// A toolkit encapsulation to interact/communicate with AWS SDK (AWS CloudWatch Logs SDK) 
    /// </summary>
    public interface ICloudWatchLogsRepository
    {
        Task<Tuple<string, List<LogGroup>>> GetLogGroupsAsync(GetLogGroupsRequest logGroupsRequest,
            CancellationToken cancelToken);

        Task<Tuple<string, List<LogStream>>> GetLogStreamsOrderByTimeAsync(GetLogStreamsRequest logStreamsRequest,
            CancellationToken cancelToken);

        Task<Tuple<string, List<LogStream>>> GetLogStreamsOrderByNameAsync(GetLogStreamsRequest logStreamsRequest,
            CancellationToken cancelToken);

        Task<Tuple<string, List<LogEvent>>> GetLogEventsAsync(GetLogEventsRequest logEventsRequest,
            CancellationToken cancelToken);

        Task<bool> DeleteLogGroupAsync(string logGroup,
            CancellationToken cancelToken);
    }
}
