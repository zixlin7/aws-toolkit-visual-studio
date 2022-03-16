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
        Task<PaginatedLogResponse<LogGroup>> GetLogGroupsAsync(GetLogGroupsRequest logGroupsRequest,
            CancellationToken cancelToken);

        Task<PaginatedLogResponse<LogStream>> GetLogStreamsAsync(GetLogStreamsRequest logStreamsRequest,
            CancellationToken cancelToken);

        Task<PaginatedLogResponse<LogEvent>> GetLogEventsAsync(GetLogEventsRequest logEventsRequest,
            CancellationToken cancelToken);

        Task<bool> DeleteLogGroupAsync(string logGroup,
            CancellationToken cancelToken);
    }
}
