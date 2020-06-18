using System;

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
    }
}