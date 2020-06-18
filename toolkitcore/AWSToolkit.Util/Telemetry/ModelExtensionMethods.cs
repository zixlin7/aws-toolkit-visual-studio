using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.ToolkitTelemetry;
using Amazon.ToolkitTelemetry.Model;

namespace Amazon.AWSToolkit.Telemetry
{
    public static class ModelExtensionMethods
    {
        // The only valid characters are alphanumeric, underscore, plus, minus, period, colon
        // The caret inverses to identify anything that is not a valid character
        static readonly Regex MetricNameIllegalChars = new Regex("[^\\w\\+\\-\\.\\:]");

        public static IList<MetricDatum> AsMetricDatums(this TelemetryEvent telemetryEvent)
        {
            long epochTimestamp = new DateTimeOffset(telemetryEvent.CreatedOn).ToUnixTimeMilliseconds();

            return telemetryEvent.Data.Select(eventData => new MetricDatum()
            {
                MetricName = eventData.MetricName,
                Unit = eventData.Unit ?? Unit.None,
                Value = eventData.Value,
                EpochTimestamp = epochTimestamp,
                Metadata = eventData.Metadata,
            }).ToList();
        }

        public static IList<MetricDatum> AsMetricDatums(this IEnumerable<TelemetryEvent> telemetryEvents)
        {
            return telemetryEvents
                .SelectMany(telemetryEvent => telemetryEvent.AsMetricDatums())
                .ToList();
        }

        public static void ApplyTo(this ProductEnvironment productEnvironment,
            Amazon.ToolkitTelemetry.Model.PostMetricsRequest request)
        {
            request.AWSProduct = productEnvironment.AwsProduct;
            request.AWSProductVersion = productEnvironment.AwsProductVersion;
            request.OS = productEnvironment.OperatingSystem;
            request.OSVersion = productEnvironment.OperatingSystemVersion;
            request.ParentProduct = productEnvironment.ParentProduct;
            request.ParentProductVersion = productEnvironment.ParentProductVersion;
        }

        /// <summary>
        /// Cleans up properties on the given event.
        /// Removes invalid MetricDatums
        /// </summary>
        public static void Sanitize(this TelemetryEvent telemetryEvent)
        {
            telemetryEvent.Data?.ToList().ForEach(Sanitize);

            // Remove any invalid data
            telemetryEvent.Data?
                .Where(d => !d.IsValid())
                .ToList()
                .ForEach(d => telemetryEvent.Data?.Remove(d));
        }

        /// <summary>
        /// Cleans up properties on the given datum.
        /// This method cannot make an invalid datum valid.
        /// </summary>
        public static void Sanitize(this MetricDatum metricDatum)
        {
            if (metricDatum.Unit == null)
            {
                metricDatum.Unit = Unit.None;
            }

            if (!string.IsNullOrEmpty(metricDatum.MetricName) && MetricNameIllegalChars.IsMatch(metricDatum.MetricName))
            {
                metricDatum.MetricName = MetricNameIllegalChars.Replace(metricDatum.MetricName, string.Empty);
            }

            // Remove any blank metadata entries
            metricDatum.Metadata?
                .RemoveAll(data => string.IsNullOrEmpty(data.Key) && string.IsNullOrEmpty(data.Value));
        }

        public static bool IsValid(this TelemetryEvent telemetryEvent)
        {
            // No Data
            if (telemetryEvent.Data == null || telemetryEvent.Data.Count == 0)
            {
                return false;
            }

            return telemetryEvent.Data.All(data => data.IsValid());
        }

        public static bool IsValid(this MetricDatum metricDatum)
        {
            return !string.IsNullOrEmpty(metricDatum.MetricName);
        }
    }
}