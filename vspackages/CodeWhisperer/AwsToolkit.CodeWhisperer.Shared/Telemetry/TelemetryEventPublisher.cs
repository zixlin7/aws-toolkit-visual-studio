using System;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Context;

namespace Amazon.AwsToolkit.CodeWhisperer.Telemetry
{
    /// <summary>
    /// Core (not tied to a specific service) implementation that can be used by MEF components responsible for handling telemetry events from language servers.
    ///
    /// Each service should derive this class, and implement the hooks to handle telemetry events.
    /// The implementations should then be MEF imported in a manner that allows them to self-register with
    /// the language client's events.
    /// </summary>
    internal abstract class TelemetryEventPublisher : IDisposable
    {
        private readonly IToolkitLspClient _lspClient;
        private readonly IToolkitContextProvider _toolkitContextProvider;

        protected TelemetryEventPublisher(IToolkitLspClient lspClient, IToolkitContextProvider toolkitContextProvider)
        {
            _lspClient = lspClient;
            _toolkitContextProvider = toolkitContextProvider;
            _lspClient.TelemetryEventNotification += OnLspClientTelemetryEventNotification;
        }

        /// <summary>
        /// Handles when the language server has sent the `telemetry/event` notification 
        /// The metric datum is constructed and recorded with the telemetry logger <see cref="ITelemetryLogger"/>
        /// </summary>
        private void OnLspClientTelemetryEventNotification(object sender, TelemetryEventArgs args)
        {
            var metricEvent = args.MetricEvent;
            if (metricEvent != null)
            {
                var metricDatum = CreateMetricDatumWithRequiredData(metricEvent.Name);
                _toolkitContextProvider.GetToolkitContext().TelemetryLogger
                    .TransformAndRecordEvent(metricDatum, metricEvent);
            }
        }

        /// <summary>
        /// Hook for service implementations to create the metric with required data
        /// </summary>
        /// <param name="metricName"> Populate with metadata that is specific to given service specific metric name</param>
        internal abstract MetricDatum CreateMetricDatumWithRequiredData(string metricName);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lspClient.TelemetryEventNotification -= OnLspClientTelemetryEventNotification;
            }
        }
    }
}
