namespace Amazon.AWSToolkit.CloudWatch.Models
{
    /// <summary>
    /// Toolkit representation of a get log streams request
    /// </summary>
    public class GetLogStreamsRequest
    {
        public string LogGroup { get; set; } = string.Empty;

        public string NextToken { get; set; }

        public string FilterText { get; set; } = string.Empty;
    }
}
