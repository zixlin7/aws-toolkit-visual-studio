using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Util;
using Amazon.ToolkitTelemetry;
using log4net;

namespace Amazon.AWSToolkit.Telemetry.Internal
{
    /// <summary>
    /// Responsible for taking metrics off the queue and sending over the wire.
    ///
    /// Transmission does not start until Initialize is called, and stops when Dispose is called.
    ///
    /// Transmission takes place in a loop on a background thread. An outer loop (PublisherLoop)
    /// checks whether or not metrics should be sent, then sends them in an inner loop (Publish).
    ///
    /// The outer loop criteria is based on size (at least <see cref="QUEUE_SIZE_THRESHOLD"/>
    /// entries queued) and on time (at least <see cref="MAX_PUBLISH_INTERVAL"/> since
    /// last transmission).
    /// 
    /// The inner loop sends metrics until: the queue is empty, a service error is detected, or
    /// resending the same metric is detected. Once the inner loop completes, the next cycle
    /// of the outer loop proceeds after waiting (<see cref="DEFAULT_LOOP_MS"/>).
    ///
    /// <seealso cref="https://github.com/aws/aws-toolkit-common/blob/master/telemetry/design/toolkit-telemetry.md"/>
    /// </summary>
    public class TelemetryPublisher : ITelemetryPublisher, IDisposable
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TelemetryPublisher));
        public static readonly TimeSpan MAX_PUBLISH_INTERVAL = new TimeSpan(0, 5, 0);
        private const int DEFAULT_LOOP_MS = 20_000; // Default amount of time spent between publish loops
        public const int QUEUE_SIZE_THRESHOLD = 12; // Publish if telemetry contains at least this many items
        public const int MAX_BATCH_SIZE = 20; // Service constraint

        private bool _disposed = false;
        private CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();
        private volatile ConcurrentQueue<TelemetryEvent> _eventQueue;
        private readonly Guid _clientId;
        private ITelemetryClient _telemetryClient;

        private DateTime _lastPublishedOn;
        private int _backoffLevel = 0;
        private TelemetryEvent _resentEventMarker;
        private bool _isShuttingDown = false;

        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// Affects whether or not transmission is allowed.
        /// </summary>
        public bool IsTelemetryEnabled { get; set; }

        /// <summary>
        /// Fired at the end of an interval where at least one publish attempt was made
        /// </summary>
        public event EventHandler MetricsPublished;

        /// <summary>
        /// Fired at the end of an interval where Publishing was skipped
        /// </summary>
        public event EventHandler PublishIntervalSkipped;

        /// <summary>
        /// Whether or not metrics transmission should take place.
        /// </summary>
        private bool IsPublishRequired
        {
            get
            {
                if (!IsTelemetryEnabled)
                {
                    return false;
                }

                if (_isShuttingDown)
                {
                    return false;
                }

                if (_eventQueue.IsEmpty)
                {
                    return false;
                }

                if (_eventQueue.Count >= QUEUE_SIZE_THRESHOLD)
                {
                    return true;
                }

                if (_lastPublishedOn.Add(MAX_PUBLISH_INTERVAL) <= _timeProvider.GetCurrentTime())
                {
                    return true;
                }

                return false;
            }
        }

        public TelemetryPublisher(ConcurrentQueue<TelemetryEvent> eventQueue, Guid clientId)
            : this(eventQueue, clientId, new TimeProvider())
        {
        }

        // Overload for testing purposes
        public TelemetryPublisher(
            ConcurrentQueue<TelemetryEvent> eventQueue,
            Guid clientId,
            TimeProvider timeProvider
        )
        {
            _eventQueue = eventQueue;
            _clientId = clientId;
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// Starts the transmission intervals
        /// </summary>
        public void Initialize(ITelemetryClient telemetryClient)
        {
            if (_telemetryClient != null)
            {
                throw new Exception("TelemetryPublisher already initialized");
            }

            _telemetryClient = telemetryClient;
            StartPublisherLoop();

            LOGGER.Debug("TelemetryPublisher Timer started");
        }

        /// <summary>
        /// Starts the Publisher loop on a background thread
        /// </summary>
        private void StartPublisherLoop()
        {
            try
            {
                ThreadPool.QueueUserWorkItem(async state => { await PublisherLoop(); });
            }
            catch (Exception e)
            {
                LOGGER.Error("Error starting up Telemetry publisher loop.", e);
            }
        }

        /// <summary>
        /// This loop is expected to run in an background thread
        /// </summary>
        private async Task PublisherLoop()
        {
            try
            {
                LOGGER.Debug("Starting Telemetry Publisher loop.");

                while (!_isShuttingDown)
                {
                    if (!IsPublishRequired)
                    {
                        OnPublishIntervalSkipped();
                        _backoffLevel = 0;
                    }
                    else
                    {
                        await Publish().ContinueWith(task => OnMetricsPublished());
                    }

                    await _timeProvider.Delay((1 + _backoffLevel) * DEFAULT_LOOP_MS, _shutdownTokenSource.Token);
                }
            }
            catch (TaskCanceledException e)
            {
                // Do nothing - we expect shut down to cancel _shutdownTokenSource
            }
            catch (Exception e)
            {
                LOGGER.Error("Telemetry publisher loop encountered an error and is now halted", e);
            }
            finally
            {
                LOGGER.Debug("Telemetry Publisher loop has stopped.");
            }
        }

        /// <summary>
        /// At this point, any events in the queue will not be sent.
        /// </summary>
        private void Shutdown()
        {
            _isShuttingDown = true;
            _shutdownTokenSource.Cancel();
        }

        /// <summary>
        /// Transmits queued metrics in batches
        /// </summary>
        /// <returns>The number of metrics transmitted</returns>
        private async Task<int> Publish()
        {
            int eventsPublished = 0;

            if (!IsTelemetryEnabled)
            {
                return eventsPublished;
            }

            try
            {
                _resentEventMarker = null;
                bool metricResendDetected = false;

                while (!_isShuttingDown && _eventQueue.Count > 0 && !metricResendDetected)
                {
                    // Break up queued metrics into batches - the service has an upper limit on request size
                    var batch = GetEventBatch();
                    if (!batch.Any())
                    {
                        break;
                    }

                    // If we detect that a metric is being re-sent, stop looping after
                    // publishing this batch. We still publish this batch, because it 
                    // may contain events that are being sent for the first time this loop.
                    metricResendDetected = _resentEventMarker != null && batch.Contains(_resentEventMarker);
                    eventsPublished += batch.Count;

                    if (!await Publish(batch))
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Telemetry Publish error", e);
            }

            _resentEventMarker = null;
            return eventsPublished;
        }

        /// <summary>
        /// Transmits a batch of metrics
        /// </summary>
        /// <returns>true: the batch was transmitted, or otherwise successfully handled, false: the batch was not successfully handled.</returns>
        private async Task<bool> Publish(List<TelemetryEvent> batch)
        {
            bool backoffNextLoop = false;
            bool requeueBatch = false;

            try
            {
                LOGGER.Debug($"Publishing {batch.Count} event(s)");
                await _telemetryClient.PostMetrics(_clientId, batch);
            }
            catch (AmazonToolkitTelemetryException e)
            {
                LOGGER.Error("Telemetry Publish error", e);

                if (e.StatusCode.Is4xx())
                {
                    // Something in the batch is bad, throw it out
                    LOGGER.Error("Discarding telemetry batch");
                }
                else
                {
                    // We don't believe the batch is invalid, place items back in the queue to try again
                    requeueBatch = true;

                    // If we saw 5xx errors, wait an interval before trying again
                    if (e.StatusCode.Is5xx())
                    {
                        backoffNextLoop = true;

                        // Stop processing
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                // Unexpected error (for example, user is offline)
                LOGGER.Error("Telemetry Publish error", e);

                // Place items back in the queue to try again
                requeueBatch = true;
            }
            finally
            {
                if (requeueBatch)
                {
                    batch.ForEach(_eventQueue.Enqueue);
                    if (_resentEventMarker == null)
                    {
                        _resentEventMarker = batch.FirstOrDefault();
                    }
                }

                if (backoffNextLoop)
                {
                    _backoffLevel++;
                }
                else
                {
                    _backoffLevel = 0;
                }
            }

            return true;
        }

        /// <summary>
        /// Pops a group of metrics from the queue and returns them
        /// </summary>
        private List<TelemetryEvent> GetEventBatch()
        {
            var batch = new List<TelemetryEvent>();

            while (batch.Count < MAX_BATCH_SIZE && !_isShuttingDown)
            {
                if (!_eventQueue.TryDequeue(out var telemetryEvent))
                {
                    break;
                }

                telemetryEvent.Sanitize();

                if (!telemetryEvent.IsValid())
                {
                    continue;
                }

                batch.Add(telemetryEvent);
            }

            return batch;
        }

        private void OnMetricsPublished()
        {
            _lastPublishedOn = _timeProvider.GetCurrentTime();
            MetricsPublished?.Invoke(this, new EventArgs());
        }

        private void OnPublishIntervalSkipped()
        {
            PublishIntervalSkipped?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Stops future metrics transmissions
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                Shutdown();
                _shutdownTokenSource.Dispose();
            }
            catch (Exception e)
            {
                LOGGER.Error("TelemetryPublisher Dispose failure", e);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}