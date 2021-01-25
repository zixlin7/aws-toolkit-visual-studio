using Amazon.AWSToolkit.Telemetry.Internal;

namespace Amazon.AWSToolkit.ResourceFetchers
{
    public class HttpResourceFetcherOptions
    {
        public ITelemetryPublisher TelemetryPublisher { get; set; }
    }
}
