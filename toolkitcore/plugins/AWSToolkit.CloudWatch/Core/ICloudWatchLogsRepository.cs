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
        Task<Tuple<string, List<LogGroup>>> GetLogGroupsAsync(CloudWatchLogsProperties logsProperties,
            CancellationToken cancelToken);

        Task<Tuple<string, List<LogStream>>> GetLogStreamsOrderByTimeAsync(CloudWatchLogsProperties logsProperties,
            CancellationToken cancelToken);

        Task<Tuple<string, List<LogStream>>> GetLogStreamsOrderByNameAsync(CloudWatchLogsProperties logsProperties,
            CancellationToken cancelToken);

        Task<Tuple<string, List<LogEvent>>> GetLogEventsAsync(CloudWatchLogsProperties logsProperties,
            CancellationToken cancelToken);

        Task<bool> DeleteLogGroupAsync(CloudWatchLogsProperties logsProperties,
            CancellationToken cancelToken);
    }
}
