using Amazon.AwsToolkit.Telemetry.Events.Core;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    public class HttpResourceFetcherOptions
    {
        public ITelemetryLogger TelemetryLogger { get; set; }
    }
}
