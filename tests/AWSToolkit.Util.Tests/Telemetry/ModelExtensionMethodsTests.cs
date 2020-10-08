using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Telemetry
{
    public class ModelExtensionMethodsTests
    {
        [Fact]
        public void AsMetricDatums_EpochTimestamp()
        {
            var telemetryMetric = new Metrics()
            {
                CreatedOn = DateTime.Now,
                Data = new List<MetricDatum>() {TestHelper.CreateSampleMetricDatum(2)}
            };

            var timestamp = new DateTimeOffset(telemetryMetric.CreatedOn).ToUnixTimeMilliseconds();

            var metricDatums = telemetryMetric.AsMetricDatums();
            Assert.Equal(telemetryMetric.Data.ToList().Count, metricDatums.Count(x => x.EpochTimestamp == timestamp));
        }

        [Fact]
        public void AsMetricDatums_Passive()
        {
            var telemetryMetric = new Metrics()
            {
                CreatedOn = DateTime.Now,
                Data = new List<MetricDatum>()
                {
                    TestHelper.CreateSampleMetricDatum(2),
                    TestHelper.CreateSampleMetricDatum(2),
                }
            };

            telemetryMetric.Data[0].Passive = false;
            telemetryMetric.Data[1].Passive = true;

            var metricDatums = telemetryMetric.AsMetricDatums();
            Assert.False(metricDatums[0].Passive);
            Assert.True(metricDatums[1].Passive);
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

            var telemetryMetric = new Metrics()
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

            telemetryMetric.Sanitize();

            Assert.Equal(2, telemetryMetric.Data.Count);
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
            };
            datum.AddMetadata("hello", "world");
            datum.AddMetadata("", null);
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