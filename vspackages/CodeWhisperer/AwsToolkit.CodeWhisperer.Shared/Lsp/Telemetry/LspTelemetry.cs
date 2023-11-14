using System.Collections.Generic;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Telemetry
{
    internal static class TelemetryMessageNames
    {
        /// <summary>
        /// Telemetry event notification sent by the language server
        /// </summary>
        public const string TelemetryNotification = "telemetry/event";
    }

    public class MetricEvent
    {
        /// <summary>
        /// The name of the event
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optional
        /// 
        /// Contains key-value pairs of relevant data associated with a metric event
        /// </summary>
        public Dictionary<string, object> Data { get; set; }

        /// <summary>
        /// Optional
        /// 
        /// The result of an event
        /// </summary>
        public ResultType? Result { get; set; }

        /// <summary>
        /// Optional
        ///
        /// Represent error information relevant to the event
        /// </summary>
        public ErrorData ErrorData { get; set; }

    }

    public enum ResultType
    {
        Succeeded,
        Failed,
        Cancelled
    }

    public class ErrorData
    {
        public string Reason { get; set; }

        public string ErrorCode{ get; set; }
        
        public int? HttpStatusCode { get; set; }
    }
}
