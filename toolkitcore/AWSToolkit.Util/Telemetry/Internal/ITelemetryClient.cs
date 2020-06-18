using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.Telemetry.Internal
{
    public interface ITelemetryClient : IDisposable
    {
        /// <summary>
        /// Sends telemetry events
        /// </summary>
        Task PostMetrics(
            Guid clientId,
            IList<TelemetryEvent> telemetryEvents,
            CancellationToken cancellationToken = default(CancellationToken)
        );

        /// <summary>
        /// Sends telemetry events
        /// Overloaded event to allow sending events from older sessions
        /// </summary>
        Task PostMetrics(
            PostMetricsRequest request,
            CancellationToken cancellationToken = default(CancellationToken)
        );
    }
}