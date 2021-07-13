using System;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.CognitoIdentity;
using log4net;
using Amazon.AwsToolkit.Telemetry.Events.Core;

namespace Amazon.AWSToolkit.VisualStudio.Telemetry
{
    /// <summary>
    /// Manages the lifecycle of Toolkit Telemetry, and exposes a metrics logger for the
    /// rest of the toolkit to use.
    /// </summary>
    public class TelemetryManager : IDisposable
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TelemetryManager));

        /// <summary>
        /// Cognito Identity Pool to use with the Telemetry Service 
        /// </summary>
#if DEBUG
        public const string TELEMETRY_COGNITO_IDENTITY_POOL = "us-east-1:db7bfc9f-8ecd-4fbb-bea7-280c16069a99";
#else
        public const string TELEMETRY_COGNITO_IDENTITY_POOL = "us-east-1:820fd6d1-95c0-4ca4-bffb-3f01d32da842"; // prod
#endif

        private bool _disposed = false;

        private TelemetryService _telemetryService;
        private ToolkitSettingsWatcher _settingsWatcher;

        public ITelemetryLogger TelemetryLogger => _telemetryService;

        public TelemetryManager(ProductEnvironment productEnvironment, MetricsOutputWindow metricsOutputWindow)
        {
            _telemetryService = new TelemetryService(productEnvironment, metricsOutputWindow);

            SetupServiceEnabledState();
        }

        /// <summary>
        /// Obtains telemetry credentials from cognito, and initializes the telemetry service.
        /// Prior to calling this, metrics can be queued. Once initialized, metrics will be transmitted.
        /// </summary>
        /// <remarks>
        /// Getting credentials can cause blocking. Callers should invoke this method on a non-UI thread.
        /// </remarks>
        public void Initialize()
        {
            try
            {
                LOGGER.Debug("Initializing Telemetry Service");
                var credentials = new CognitoAWSCredentials(TELEMETRY_COGNITO_IDENTITY_POOL, RegionEndpoint.USEast1);
                _telemetryService.Initialize(credentials, ClientId.Instance.Get());
                LOGGER.Debug("Telemetry Service Initialized");
            }
            catch (Exception e)
            {
                LOGGER.Error("Error during TelemetryManager initialization", e);
            }
        }

        public void SetAccountId(string accountId)
        {
            _telemetryService.SetAccountId(accountId);
        }

        public void Dispose()
        {
            try
            {
                if (_disposed)
                {
                    return;
                }

                if (_settingsWatcher != null)
                {
                    _settingsWatcher.SettingsChanged -= SettingsChangedHandler;

                    _settingsWatcher.Dispose();
                    _settingsWatcher = null;
                }

                if (_telemetryService != null)
                {
                    _telemetryService.Dispose();
                    _telemetryService = null;
                }
            }
            catch (Exception e)
            {
                LOGGER.Error(e);
            }
            finally
            {
                _disposed = true;
            }
        }

        /// <summary>
        /// Enables/Disables the service whenever the setting changes.
        /// Initializes the service to the current setting value.
        /// </summary>
        private void SetupServiceEnabledState()
        {
            _settingsWatcher = new ToolkitSettingsWatcher();
            _settingsWatcher.SettingsChanged += SettingsChangedHandler;

            // Initialize the enabled state
            UpdateServiceEnabledState();
        }

        private void SettingsChangedHandler(object sender, EventArgs e)
        {
            UpdateServiceEnabledState();
        }

        /// <summary>
        /// Enables or Disables the service based on current state
        /// </summary>
        private void UpdateServiceEnabledState()
        {
            if (IsTelemetryEnabled())
            {
                _telemetryService.Enable();
            }
            else
            {
                _telemetryService.Disable();
            }
        }

        private bool IsTelemetryEnabled()
        {
            return ToolkitSettings.Instance.TelemetryEnabled;
        }
    }
}
