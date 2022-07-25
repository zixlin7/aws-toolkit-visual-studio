using System;
using System.Linq;

using Amazon.AWSToolkit.Settings;
using log4net;

namespace Amazon.AWSToolkit.Telemetry
{
    /// <summary>
    /// Represents a unique identifier for this toolkit installation.
    /// An Id is created and persisted when necessary.
    /// </summary>
    public sealed class ClientId : IEquatable<ClientId>
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(ClientId));

        public static readonly ClientId Instance;

        public static readonly ClientId AutomatedTestClientId;

        public static readonly ClientId TelemetryOptOutClientId;

        // The service does not accept Guid.Empty, so we must use a fixed Guid to represent this fallback scenario.
        public static readonly ClientId UnknownClientId;

        static ClientId()
        {
            AutomatedTestClientId = new ClientId("ffffffff-ffff-ffff-ffff-ffffffffffff");
            TelemetryOptOutClientId = new ClientId("11111111-1111-1111-1111-111111111111");
            UnknownClientId = new ClientId("00000000-0000-0000-0000-000000000000");

            Instance = new ClientId();
        }

        private readonly string _clientId;

        private ClientId()
        {
            try
            {
                _clientId = IsXunitRunning() ? AutomatedTestClientId : FromConfiguration();
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to get a ClientId. Specifying unknown.", ex);
                _clientId = UnknownClientId;
            }
        }

        private ClientId(string clientId)
        {
            _clientId = clientId;
        }

        /// <summary>
        /// Returns the effective client ID.
        /// </summary>
        /// <remarks>
        /// The stored client ID is returned when telemetry is enabled, otherwise the
        /// <see cref="TelemetryOptOutClientId"/> is returned.  If telemetry is re-enabled,
        /// the previously existing stored client ID will be returned.
        /// </remarks>
        public string Value
        {
            get
            {
                // Don't use implicit string cast here or it will create a cycle
                if (_clientId == AutomatedTestClientId._clientId)
                {
                    return _clientId;
                }

                return ToolkitSettings.Instance.TelemetryEnabled? _clientId : TelemetryOptOutClientId._clientId;
            }
        }

        public bool Equals(ClientId other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ClientId) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(ClientId @this) => @this.Value;

        private static string FromConfiguration()
        {
            var cliendId = ToolkitSettings.Instance.TelemetryClientId;

            if (cliendId == null)
            {
                cliendId = Guid.NewGuid();
                ToolkitSettings.Instance.TelemetryClientId = cliendId;
            }

            return cliendId.Value.ToString();
        }

        private static bool IsXunitRunning()
        {
            return AppDomain.CurrentDomain.GetAssemblies().AsEnumerable()
                .FirstOrDefault(a => a.FullName.ToLowerInvariant().StartsWith("xunit")) != null;
        }
    }
}
