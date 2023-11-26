using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Telemetry;
using Amazon.AwsToolkit.CodeWhisperer.Telemetry;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Clients;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Telemetry
{
    public class CodeWhispererTelemetryEventPublisherTests : IDisposable
    {
        private readonly FakeCodeWhispererClient _lspClient = new FakeCodeWhispererClient();
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly CodeWhispererTelemetryEventPublisher _sut;

        private readonly MetricEvent _sampleMetricEvent = new MetricEvent()
        {
            Name = "sample-metric",
            Data = new Dictionary<string, object>() { { "duration", 123 }, { "url", "http://abc.com" } }
        };

        private MetricDatum _recordedMetric;

        public CodeWhispererTelemetryEventPublisherTests()
        {
            _contextFixture.SetupTelemetryCallback(metrics =>
            {
                var datum = metrics.Data.FirstOrDefault();
                if (datum != null)
                {
                    _recordedMetric = datum;
                }
            });
            _sut = new CodeWhispererTelemetryEventPublisher(_lspClient, _contextFixture.ToolkitContextProvider);
        }

        [Fact]
        public void OnTelemetryEventNotification()
        {
            var eventArgs = new TelemetryEventArgs() { MetricEvent = _sampleMetricEvent };

            _lspClient.RaiseTelemetryEvent(eventArgs);

            _recordedMetric.MetricName.Should().BeEquivalentTo(_sampleMetricEvent.Name);
            //verify additional metadata for awsAccount is added
            _recordedMetric.Metadata.Should().HaveCount(_sampleMetricEvent.Data.Count + 1);
            _recordedMetric.Metadata[MetadataKeys.AwsAccount].Should().BeEquivalentTo(MetadataValue.NotApplicable);
            _recordedMetric.Unit.Should().BeEquivalentTo(Unit.None);
            _recordedMetric.Passive.Should().BeTrue();
        }

        public void Dispose()
        {
            _sut?.Dispose();
        }
    }
}
