using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Amazon.AwsToolkit.Telemetry.Events.Core;

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
            };
            Enumerable.Range(1, metadataCount).ToList().ForEach(x =>
            {
                datum.Metadata[$"SomeProp{x}"] = Guid.NewGuid().ToString();
            });
            return datum;
        }

        public static void AddEventsToQueue(ConcurrentQueue<Metrics> queue, int quantity)
        {
            for (int i = 0; i < quantity; i++)
            {
                var telemetryEvent = new Metrics()
                {
                    CreatedOn = DateTime.Now,
                    Data = new List<MetricDatum>() {CreateSampleMetricDatum(5)}
                };

                queue.Enqueue(telemetryEvent);
            }
        }

        public static MetricDatum AddMetadata(this MetricDatum metricDatum, string key, string value)
        {
            metricDatum.Metadata.Add(key, value);

            return metricDatum;
        }
    }
}