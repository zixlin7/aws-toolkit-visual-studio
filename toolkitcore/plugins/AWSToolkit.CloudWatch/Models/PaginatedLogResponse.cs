using System.Collections.Generic;

namespace Amazon.AWSToolkit.CloudWatch.Models
{
    /// <summary>
    /// Represents a paginated response including list of values retrieved and next page token
    /// </summary>
    /// <typeparam name="T">type of values eg. log group, log stream</typeparam>
    public class PaginatedLogResponse<T>
    {
        public string NextToken { get; }

        public IEnumerable<T> Values { get; }

        public PaginatedLogResponse(string nextToken, IEnumerable<T> values)
        {
            NextToken = nextToken;
            Values = values;
        }
    }
}
