using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Telemetry;
using Amazon.ToolkitTelemetry;
using Amazon.ToolkitTelemetry.Model;
using Newtonsoft.Json;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Telemetry
{
    public class ModelExtensionMethodsTests
    {
        [Fact]
        public void AsMetricDatums()
        {
            var telemetryEvent = new TelemetryEvent()
            {
                CreatedOn = DateTime.Now,
                Data = new List<MetricDatum>() {TestHelper.CreateSampleMetricDatum(2)}
            };

            var timestamp = new DateTimeOffset(telemetryEvent.CreatedOn).ToUnixTimeMilliseconds();
            telemetryEvent.Data.ToList().ForEach(x => x.EpochTimestamp = timestamp);

            var metricDatums = telemetryEvent.AsMetricDatums();

            Assert.Equal(
                JsonConvert.SerializeObject(telemetryEvent.Data),
                JsonConvert.SerializeObject(metricDatums)
            );
        }
    }

    public class SanitizeAndValidateTests
    {
        [Fact]
        public void InvalidMetricDatum()
        {
            Assert.False(new MetricDatum()
            {
                MetricName = null
            }.IsValid());

            Assert.False(new MetricDatum()
            {
                MetricName = ""
            }.IsValid());
        }

        [Fact]
        public void ValidMetricDatum()
        {
            Assert.True(CreateCleanMetricDatum().IsValid());
        }

        [Fact]
        public void SanitizeMetricDatum()
        {
            var datum = CreateDirtyMetricDatum();

            datum.Sanitize();

            AssertDirtyDatumIsClean(datum);
        }

        [Fact]
        public void SanitizeTelemetryEvent()
        {
            var cleanMetricDatum = CreateCleanMetricDatum();
            var dirtyMetricDatum = CreateDirtyMetricDatum();

            var telemetryEvent = new TelemetryEvent()
            {
                CreatedOn = DateTime.Now,
                Data = new List<MetricDatum>()
                {
                    cleanMetricDatum,
                    dirtyMetricDatum,
                    new MetricDatum()
                    {
                        MetricName = null
                    }
                }
            };

            telemetryEvent.Sanitize();

            Assert.Equal(2, telemetryEvent.Data.Count);
            AssertDirtyDatumIsClean(dirtyMetricDatum);
        }

        [Theory]
        [InlineData("foo_init", "foo_init")]
        [InlineData("foo-init", "foo-init")]
        [InlineData("foo+init", "foo+init")]
        [InlineData("foo.init", "foo.init")]
        [InlineData("foo:init", "foo:init")]
        [InlineData("FOO_INIT0123456789", "FOO_INIT0123456789")]
        [InlineData("foo;init", "fooinit")]
        [InlineData("foo init", "fooinit")]
        [InlineData("foo bar|baz!init", "foobarbazinit")]
        public void SanitizeMetricName(string metricName, string sanitizedMetricName)
        {
            var metric = CreateCleanMetricDatum();
            metric.MetricName = metricName;

            metric.Sanitize();

            Assert.Equal(sanitizedMetricName, metric.MetricName);
        }

        private static MetricDatum CreateCleanMetricDatum()
        {
            return new MetricDatum()
            {
                MetricName = "bees",
                Unit = Unit.None,
                Value = 0
            };
        }

        private static MetricDatum CreateDirtyMetricDatum()
        {
            var datum = new MetricDatum()
                {
                    MetricName = "hello world",
                    Unit = null,
                }
                .AddMetadata("hello", "world")
                .AddMetadata("", null);
            return datum;
        }

        private static void AssertDirtyDatumIsClean(MetricDatum datum)
        {
            Assert.Equal("helloworld", datum.MetricName);
            Assert.Equal(Unit.None, datum.Unit);
            Assert.Single(datum.Metadata);
            Assert.Equal("hello", datum.Metadata.First().Key);
        }

    }
}