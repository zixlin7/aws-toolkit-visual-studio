using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.Telemetry
{
    public class ErrorMetadata
    {
        public string ErrorCode { get; set; }
        public string Reason { get; set; }
        public CausedBy CausedBy { get; set; }
        public string HttpStatusCode { get; set; }
        public string RequestId { get; set; }
        public string RequestServiceType { get; set; }
    }
}
