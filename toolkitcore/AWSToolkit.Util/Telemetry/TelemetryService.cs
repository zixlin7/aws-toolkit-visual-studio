using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Telemetry.Internal;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.Runtime;
using Amazon.ToolkitTelemetry.Model;
using log4net;

namespace Amazon.AWSToolkit.Telemetry
{
    /// <summary>
    /// Used by the Toolkit to send usage metrics.
    /// Metrics are queued, and a publisher manages the transmission.
    /// 
    /// To use:
    /// During Toolkit startup:
    /// - instantiate
    /// - call Disable/Enable based on current settings
    /// - call Initialize to start transmissions
    ///
    /// During Toolkit lifetime:
    /// - call SetAccountId in response to account/credentials changes the user makes
    /// - call Disable/Enable in response to settings changes the user makes
    /// - call Record to send metrics (queues for transmission)
    /// 
    /// During Toolkit shutdown:
    /// - call Dispose to end transmissions and clean up
    /// </summary>
    public class TelemetryService : ITelemetryLogger, IDisposable
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TelemetryService));
        
#if DEBUG
        const string DefaultTelemetryEndpoint = "https://7zftft3lj2.execute-api.us-east-1.amazonaws.com/Beta";
#else
        const string DefaultTelemetryEndpoint = "https://client-telemetry.us-east-1.amazonaws.com";
#endif

        private readonly ConcurrentQueue<TelemetryEvent> _eventQueue;
        private ProductEnvironment _productEnvironment;
        private ITelemetryClient _telemetryClient;
        private ITelemetryPublisher _telemetryPublisher;
        private string _accountId = string.Empty;

        private bool _disposed = false;
        private bool _isTelemetryEnabled = false;

        public TelemetryService(ProductEnvironment productEnvironment) :
            this(new ConcurrentQueue<TelemetryEvent>(), productEnvironment)
        {
        }

        public TelemetryService(ConcurrentQueue<TelemetryEvent> eventQueue, ProductEnvironment productEnvironment)
        {
            _eventQueue = eventQueue;
            _productEnvironment = productEnvironment;
        }

        /// <summary>
        /// Sets up the service to start sending metrics.
        /// Metrics are not sent until this is called.
        /// </summary>
        public void Initialize(AWSCredentials credentials, Guid clientId)
        {
            var telemetryPublisher = new TelemetryPublisher(_eventQueue, clientId);
            var telemetryClient = new TelemetryClient(credentials, DefaultTelemetryEndpoint, _productEnvironment);
            Initialize(clientId, telemetryClient, telemetryPublisher);
        }

        /// <summary>
        /// Sets up the service to start sending metrics.
        /// Overload - used for testing purposes.
        /// </summary>
        public void Initialize(Guid clientId, ITelemetryClient telemetryClient, ITelemetryPublisher telemetryPublisher)
        {
            _telemetryClient = telemetryClient;

            _telemetryPublisher = telemetryPublisher;
            _telemetryPublisher.IsTelemetryEnabled = _isTelemetryEnabled;

            _telemetryPublisher.Initialize(_telemetryClient);
        }

        /// <summary>
        /// Allows the service to queue metrics and transmit them.
        /// </summary>
        public void Enable()
        {
            _isTelemetryEnabled = true;

            if (_telemetryPublisher != null)
            {
                _telemetryPublisher.IsTelemetryEnabled = _isTelemetryEnabled;
            }

            LOGGER.Debug("Telemetry service enabled");
        }

        /// <summary>
        /// Prohibits the service from queuing metrics and transmitting them.
        /// Clears any queued metrics.
        /// </summary>
        public void Disable()
        {
            _isTelemetryEnabled = false;

            if (_telemetryPublisher != null)
            {
                _telemetryPublisher.IsTelemetryEnabled = _isTelemetryEnabled;
            }

            EmptyQueue();
            LOGGER.Debug("Telemetry service disabled");
        }

        /// <summary>
        /// Sets the Account Id to be applied to any subsequent metrics that are recorded
        /// that do not already have one set.
        /// </summary>
        public void SetAccountId(string accountId)
        {
            _accountId = accountId;
        }

        /// <summary>
        /// Queues metrics for transmission.
        /// </summary>
        public void Record(TelemetryEvent telemetryEvent)
        {
            if (!_isTelemetryEnabled)
            {
                return;
            }

            ApplyMissingMetadata(telemetryEvent);

            _eventQueue.Enqueue(telemetryEvent);
        }

        /// <summary>
        /// Shuts down the Service and cleans up
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                if (_telemetryPublisher != null)
                {
                    _telemetryPublisher.Dispose();
                    _telemetryPublisher = null;
                }

                if (_telemetryClient != null)
                {
                    _telemetryClient.Dispose();
                    _telemetryClient = null;
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("TelemetryService Dispose failure", e);
            }
            finally
            {
                _disposed = true;
            }
        }

        private void EmptyQueue()
        {
            while (_eventQueue.Count > 0)
            {
                _eventQueue.TryDequeue(out _);
            }
        }

        private void ApplyMissingMetadata(TelemetryEvent telemetryEvent)
        {
            if (string.IsNullOrEmpty(_accountId))
            {
                return;
            }

            telemetryEvent.Data?.ToList().ForEach(ApplyMissingMetadata);
        }

        private void ApplyMissingMetadata(MetricDatum metricDatum)
        {
            if (string.IsNullOrEmpty(_accountId))
            {
                return;
            }

            EnsureAccountMetadataExists(metricDatum);

            var accountEntry = metricDatum.Metadata
                .First(entry => entry.Key == MetadataKeys.AwsAccount);

            if (string.IsNullOrWhiteSpace(accountEntry.Value))
            {
                accountEntry.Value = _accountId;
            }
        }

        private static void EnsureAccountMetadataExists(MetricDatum metricDatum)
        {
            if (metricDatum.Metadata == null)
            {
                metricDatum.Metadata = new List<MetadataEntry>();
            }

            var accountEntry = metricDatum.Metadata
                .FirstOrDefault(entry => entry.Key == MetadataKeys.AwsAccount);

            if (accountEntry != null)
            {
                return;
            }

            accountEntry = new MetadataEntry()
            {
                Key = MetadataKeys.AwsAccount
            };

            metricDatum.Metadata.Add(accountEntry);
        }
    }
}