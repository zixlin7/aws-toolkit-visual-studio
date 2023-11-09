using System;
using System.Collections.Generic;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Telemetry;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Clients;
using Amazon.AWSToolkit.Context;
using Amazon.AwsToolkit.Telemetry.Events.Core;

using FluentAssertions;

using Xunit;

using Amazon.AWSToolkit.Tests.Common.Context;

using System.Linq;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Telemetry;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Telemetry
{
    internal class FakeTelemetryEventPublisher : TelemetryEventPublisher
    {
        public MetricDatum MetricDatum = new MetricDatum();

        public FakeTelemetryEventPublisher(IToolkitLspClient lspClient, IToolkitContextProvider toolkitContextProvider)
            : base(lspClient, toolkitContextProvider)
        {
        }

        internal override MetricDatum CreateMetricDatumWithRequiredData(string metricName)
        {
            MetricDatum.Passive = true;
            return MetricDatum;
        }
    }

    public class TelemetryEventPublisherTests :IDisposable
    {
        private readonly FakeToolkitLspClient _lspClient = new FakeToolkitLspClient();
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly MetricEvent _sampleMetricEvent = new MetricEvent()
        {
            Name = "sample-metric",
            Data = new Dictionary<string, object>() { { "duration", 123 }, { "url", "http://abc.com" } }
        };

        private readonly FakeTelemetryEventPublisher _sut;
        private MetricDatum _recordedMetric;

        public TelemetryEventPublisherTests()
        {
            _contextFixture.SetupTelemetryCallback(metrics =>
            {
                var datum = metrics.Data.FirstOrDefault();
                if (datum != null)
                {
                    _recordedMetric = datum;
                }
            });
            _sut = new FakeTelemetryEventPublisher(_lspClient, _contextFixture.ToolkitContextProvider);
        }

        [Fact]
        public void OnTelemetryEventNotification_Empty()
        {
            var eventArgs = new TelemetryEventArgs();

            _lspClient.RaiseTelemetryEvent(eventArgs);

            _sut.MetricDatum.Passive.Should().BeFalse();
            _recordedMetric.Should().BeNull();
        }


        [Fact]
        public void OnTelemetryEventNotification()
        {
            var eventArgs = new TelemetryEventArgs() { MetricEvent = _sampleMetricEvent };

            _lspClient.RaiseTelemetryEvent(eventArgs);

            _sut.MetricDatum.Passive.Should().BeTrue();
            _recordedMetric.MetricName.Should().BeEquivalentTo(_sampleMetricEvent.Name);
            _recordedMetric.Metadata.Should().HaveCount(_sampleMetricEvent.Data.Count);
        }

        public void Dispose()
        {
            _sut?.Dispose();
        }
    }
}
