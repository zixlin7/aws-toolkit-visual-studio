using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Telemetry;
using Amazon.ToolkitTelemetry;
using Amazon.ToolkitTelemetry.Model;

namespace Amazon.AWSToolkit.Util.Tests.Telemetry
{
    internal static class TestHelper
    {
        private static readonly Random Randomizer = new Random();

        /// <summary>
        /// Make a Sample MetricDatum containing random values
        /// </summary>
        public static MetricDatum CreateSampleMetricDatum(int metadataCount)
        {
            var datum = new MetricDatum()
            {
                MetricName = Guid.NewGuid().ToString(),
                Unit = Unit.Count,
                Value = Randomizer.NextDouble(),
                Metadata = Enumerable.Range(1, metadataCount).Select(x => new MetadataEntry()
                {
                    Key = $"SomeProp{x}",
                    Value = Guid.NewGuid().ToString()
                }).ToList()
            };

            return datum;
        }

        public static void AddEventsToQueue(ConcurrentQueue<TelemetryEvent> queue, int quantity)
        {
            for (int i = 0; i < quantity; i++)
            {
                var telemetryEvent = new TelemetryEvent()
                {
                    CreatedOn = DateTime.Now,
                    Data = new List<MetricDatum>() { CreateSampleMetricDatum(5) }
                };

                queue.Enqueue(telemetryEvent);
            }
        }

        public static MetricDatum AddMetadata(this MetricDatum metricDatum, string key, string value)
        {
            metricDatum.Metadata.Add(new MetadataEntry()
            {
                Key = key,
                Value = value
            });
        
            return metricDatum;
        }
    }
}