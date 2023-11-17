using System;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Telemetry
{
    /// <summary>
    /// Used with events signalling that a language server has sent the telemetry/event notification
    /// </summary>
    public class TelemetryEventArgs: EventArgs
    {
        /// <summary>
        /// The metric event sent from the language server
        /// </summary>
        public MetricEvent MetricEvent { get; set; }
    }
}
