using System.Collections.Generic;
using log4net;

namespace Amazon.AWSToolkit.Telemetry
{
    // Temp holding of metrics events that haven't gone to toolkit-common yet.
    public static class TelemetryEvents
    {
        private static ILog LOGGER = LogManager.GetLogger(typeof(TelemetryEvents));

        /// Utility method for generated code to add a metadata to a datum 
        /// Metadata is only added if the value is non-blank
        private static void AddMetadata(this Amazon.ToolkitTelemetry.Model.MetricDatum metricDatum, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var entry = new Amazon.ToolkitTelemetry.Model.MetadataEntry();
            entry.Key = key;
            entry.Value = value;

            metricDatum.Metadata.Add(entry);
        }

        /// Utility method for generated code to add a metadata to a datum (object overload)
        /// Metadata is only added if the value is non-blank
        private static void AddMetadata(this Amazon.ToolkitTelemetry.Model.MetricDatum metricDatum, string key, object value)
        {
            if ((value == null))
            {
                return;
            }

            metricDatum.AddMetadata(key, value.ToString());
        }

        /// Utility method for generated code to add a metadata to a datum (bool overload)
        /// Metadata is only added if the value is non-blank
        private static void AddMetadata(this Amazon.ToolkitTelemetry.Model.MetricDatum metricDatum, string key, bool value)
        {
            string valueStr = "false";
            if (value)
            {
                valueStr = "true";
            }

            metricDatum.AddMetadata(key, valueStr);
        }

        /// Utility method for generated code to add a metadata to a datum (double overload)
        /// Metadata is only added if the value is non-blank
        private static void AddMetadata(this Amazon.ToolkitTelemetry.Model.MetricDatum metricDatum, string key, double value)
        {
            metricDatum.AddMetadata(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// Utility method for generated code to add a metadata to a datum (int overload)
        /// Metadata is only added if the value is non-blank
        private static void AddMetadata(this Amazon.ToolkitTelemetry.Model.MetricDatum metricDatum, string key, int value)
        {
            metricDatum.AddMetadata(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}