namespace Amazon.AWSToolkit.CloudWatch.Models
{
    /// <summary>
    /// Toolkit representation of a get log groups request
    /// </summary>
    public class GetLogGroupsRequest
    {
        public string NextToken { get; set; }

        public string FilterText { get; set; } = string.Empty;
    }
}
