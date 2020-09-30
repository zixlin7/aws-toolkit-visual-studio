using System;
using Amazon.AWSToolkit.Settings;
using log4net;

namespace Amazon.AWSToolkit.Telemetry
{
    /// <summary>
    /// Represents a unique identifier for this toolkit installation.
    /// An Id is created and persisted when necessary.
    /// </summary>
    public class ClientId
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ClientId));

        public static ClientId Instance = new ClientId();

        // The service does not accept Guid.Empty, so we must use a fixed Guid to represent this fallback scenario.
        public static readonly Guid UnknownClientId = new Guid("ba00fe5f-16ef-4acb-9552-3750f843fe1d");

        private readonly Lazy<Guid> _clientId;

        private ClientId()
        {
            _clientId = new Lazy<Guid>(LazyGet);
        }

        public Guid Get()
        {
            return _clientId.Value;
        }

        private Guid LazyGet()
        {
            try
            {
                return GetFromPersistenceManager();
            }
            catch (Exception e)
            {
                // Unexpected failure. Use the "unknown clientId" marker.
                LOGGER.Error("Failed to get a ClientId. Specifying unknown.", e);
                return UnknownClientId;
            }
        }

        /// <summary>
        /// Retrieves ClientId from PersistenceManager.
        /// If no Id is present, one is created and saved.
        /// </summary>
        private Guid GetFromPersistenceManager()
        {
            var customerId = ToolkitSettings.Instance.TelemetryClientId;

            if (customerId == null)
            {
                customerId = Guid.NewGuid();
                ToolkitSettings.Instance.TelemetryClientId = customerId;
            }

            return customerId.Value;
        }
    }
}