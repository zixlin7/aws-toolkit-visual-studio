using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.ToolkitTelemetry.Model;
using MetricDatum = Amazon.ToolkitTelemetry.Model.MetricDatum;

namespace Amazon.AWSToolkit.Telemetry
{
    public static class ModelExtensionMethods
    {
        // The only valid characters are alphanumeric, underscore, plus, minus, period, colon
        // The caret inverses to identify anything that is not a valid character
        static readonly Regex MetricNameIllegalChars = new Regex("[^\\w\\+\\-\\.\\:]");

        public static IList<MetricDatum> AsMetricDatums(this Metrics telemetryMetric)
        {
            long epochTimestamp = new DateTimeOffset(telemetryMetric.CreatedOn).ToUnixTimeMilliseconds();

            return telemetryMetric.Data.Select(eventData => new MetricDatum()
            {
                MetricName = eventData.MetricName,
                Unit = new ToolkitTelemetry.Unit(eventData.Unit.Value),
                Value = eventData.Value,
                Passive = eventData.Passive,
                EpochTimestamp = epochTimestamp,
                Metadata = eventData.Metadata.Select(kvp => new MetadataEntry {Key = kvp.Key, Value = kvp.Value})
                    .ToList(),
            }).ToList();
        }

        public static IList<MetricDatum> AsMetricDatums(this IEnumerable<Metrics> telemetryEvents)
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
            request.OSArchitecture = productEnvironment.OperatingSystemArchitecture;
            request.OSVersion = productEnvironment.OperatingSystemVersion;
            request.ParentProduct = productEnvironment.ParentProduct;
            request.ParentProductVersion = productEnvironment.ParentProductVersion;
        }

        public static void ApplyTo(this ProductEnvironment productEnvironment,
            PostFeedbackRequest request)
        {
            request.AWSProduct = productEnvironment.AwsProduct;
            request.AWSProductVersion = productEnvironment.AwsProductVersion;
            request.OS = productEnvironment.OperatingSystem;
            request.OSVersion = productEnvironment.OperatingSystemVersion;
            request.ParentProduct = productEnvironment.ParentProduct;
            request.ParentProductVersion = productEnvironment.ParentProductVersion;
        }

        public static void ApplyTo(this IDictionary<string, string> metadata,
            PostFeedbackRequest request)
        {
            if (metadata == null)
            {
                return;
            }

            var entries = metadata.Select(entry => new MetadataEntry() { Key = entry.Key, Value = entry.Value })
                .ToList();

            var updatedMetadata = entries.Concat(request.Metadata.Where(x => !metadata.Keys.Contains(x.Key))).ToList();
            request.Metadata = updatedMetadata;
        }

        /// <summary>
        /// Cleans up properties on the given metric.
        /// Removes invalid MetricDatums
        /// </summary>
        public static void Sanitize(this Metrics telemetryMetric)
        {
            telemetryMetric.Data?.ToList().ForEach(Sanitize);

            // Remove any invalid data
            telemetryMetric.Data?
                .Where(d => !d.IsValid())
                .ToList()
                .ForEach(d => telemetryMetric.Data?.Remove(d));
        }

        /// <summary>
        /// Cleans up properties on the given datum.
        /// This method cannot make an invalid datum valid.
        /// </summary>
        public static void Sanitize(this Amazon.AwsToolkit.Telemetry.Events.Core.MetricDatum metricDatum)
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
                .Where(data => string.IsNullOrEmpty(data.Key) && string.IsNullOrEmpty(data.Value))
                .Select(data => data.Key)
                .ToList()
                .ForEach(blankKey => metricDatum.Metadata?.Remove(blankKey));
        }

        public static bool IsValid(this Metrics telemetryMetric)
        {
            // No Data
            if (telemetryMetric.Data == null || telemetryMetric.Data.Count == 0)
            {
                return false;
            }

            return telemetryMetric.Data.All(data => data.IsValid());
        }

        public static bool IsValid(this Amazon.AwsToolkit.Telemetry.Events.Core.MetricDatum metricDatum)
        {
            return !string.IsNullOrEmpty(metricDatum.MetricName);
        }
    }
}
