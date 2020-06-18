using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.Telemetry.Internal
{
    public class PostMetricsRequest
    {
        public Guid ClientId { get; set; }
        public IList<TelemetryEvent> TelemetryEvents { get; set; }
        public ProductEnvironment ProductEnvironment { get; set; }
    }
}