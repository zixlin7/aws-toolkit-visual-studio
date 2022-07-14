using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Core;

namespace Amazon.AWSToolkit.Telemetry.Internal
{
    /// <summary>
    /// Responsible for transmitting metrics to the server.
    /// </summary>
    public interface ITelemetryPublisher : IDisposable
    {
        /// <summary>
        /// Fired at the end of an interval where at least one publish attempt was made
        /// </summary>
        event EventHandler MetricsPublished;

        /// <summary>
        /// Fired at the end of an interval where Publishing was skipped
        /// </summary>
        event EventHandler PublishIntervalSkipped;

        /// <summary>
        /// Affects whether or not events are actually transmitted
        /// </summary>
        bool IsTelemetryEnabled { get; set; }

        void Initialize(ITelemetryClient telemetryClient);

        /// <summary>
        /// Sends feedback information
        /// </summary>
        /// <param name="sentiment">feedback sentiment eg. positive/negative</param>
        /// <param name="comment">feedback comment</param>
        /// <param name="metadata">additional feedback metadata</param>
        Task SendFeedback(Sentiment sentiment, string comment, IDictionary<string, string> metadata);
    }
}
