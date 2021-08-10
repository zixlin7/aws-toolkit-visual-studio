using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AwsToolkit.Telemetry.Events.Core;

namespace Amazon.AWSToolkit.Telemetry.Internal
{
    public interface ITelemetryClient : IDisposable
    {
        /// <summary>
        /// Sends telemetry metrics
        /// </summary>
        Task PostMetrics(
            Guid clientId,
            IList<Metrics> telemetryMetrics,
            CancellationToken cancellationToken = default(CancellationToken)
        );

        /// <summary>
        /// Sends telemetry metrics
        /// Overloaded metric to allow sending metrics from older sessions
        /// </summary>
        Task PostMetrics(
            PostMetricsRequest request,
            CancellationToken cancellationToken = default(CancellationToken)
        );

        /// <summary>
        /// Sends feedback information
        /// </summary>
        /// <param name="sentiment">feedback sentiment eg. positive/negative</param>
        /// <param name="comment">feedback comment</param>
        Task SendFeedback(Sentiment sentiment, string comment);
    }
}
