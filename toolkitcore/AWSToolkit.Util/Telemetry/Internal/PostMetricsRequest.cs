using System.Collections.Generic;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.Telemetry.Internal
{
    public class PostMetricsRequest
    {
        public ClientId ClientId { get; set; }
        public IList<Metrics> TelemetryMetrics { get; set; }
        public ProductEnvironment ProductEnvironment { get; set; }
    }
}
