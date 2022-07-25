﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry.Internal;
using Amazon.AWSToolkit.Telemetry.Model;
using Moq;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Telemetry
{
    public class TelemetryServiceTests
    {
        private readonly ClientId _clientId = ClientId.AutomatedTestClientId;
        private readonly ConcurrentQueue<Metrics> _eventQueue = new ConcurrentQueue<Metrics>();
        private readonly Mock<IMetricsOutputWindow> _metricsOutputWindow = new Mock<IMetricsOutputWindow>();
        private readonly Mock<ITelemetryClient> _telemetryClient = new Mock<ITelemetryClient>();
        private readonly Mock<ITelemetryPublisher> _telemetryPublisher = new Mock<ITelemetryPublisher>();

        private readonly TelemetryService _sut;

        private readonly Metrics _sampleTelemetryEvent = new Metrics()
        {
            CreatedOn = DateTime.Now,
            Data = new List<MetricDatum>() {TestHelper.CreateSampleMetricDatum(5)}
        };

        public TelemetryServiceTests()
        {
            _sut = new TelemetryService(_eventQueue, ProductEnvironment.Default, _metricsOutputWindow.Object);
        }

        [Fact]
        public void DisableAppliesToPublisher()
        {
            _sut.Initialize(_telemetryClient.Object, _telemetryPublisher.Object);

            _sut.Disable();

            _telemetryPublisher.VerifySet(mock => mock.IsTelemetryEnabled = false);
        }

        [Fact]
        public void DisableEmptiesQueue()
        {
            TestHelper.AddEventsToQueue(_eventQueue, 3);
            _sut.Disable();

            Assert.Empty(_eventQueue);
        }

        [Fact]
        public void EnableAppliesToPublisher()
        {
            _sut.Initialize(_telemetryClient.Object, _telemetryPublisher.Object);

            _sut.Enable();

            _telemetryPublisher.VerifySet(mock => mock.IsTelemetryEnabled = true);
        }

        [Fact]
        public void InitializeAppliesDisable()
        {
            _sut.Disable();
            _sut.Initialize(_telemetryClient.Object, _telemetryPublisher.Object);
            _telemetryPublisher.VerifySet(mock => mock.IsTelemetryEnabled = false);
        }

        [Fact]
        public void InitializeAppliesEnable()
        {
            _sut.Enable();
            _sut.Initialize(_telemetryClient.Object, _telemetryPublisher.Object);
            _telemetryPublisher.VerifySet(mock => mock.IsTelemetryEnabled = true);
        }

        [Fact]
        public void RecordWhenEnabled()
        {
            _sut.Enable();
            _sut.Record(_sampleTelemetryEvent);

            Assert.Single(_eventQueue);
        }

        [Fact]
        public void RecordWhenDisabled()
        {
            _sut.Disable();
            _sut.Record(_sampleTelemetryEvent);

            Assert.Empty(_eventQueue);
        }

        [Fact]
        public void RecordPreservesAccountId()
        {
            var accountId = Guid.NewGuid().ToString();
            var eventAccountId = Guid.NewGuid().ToString();

            _sampleTelemetryEvent.Data.ToList().ForEach(datum =>
            {
                datum.AddMetadata(MetadataKeys.AwsAccount, eventAccountId);
            });

            _sut.Enable();
            _sut.SetAccountId(accountId);

            _sut.Record(_sampleTelemetryEvent);

            var queuedEvent = _eventQueue.First();
            Assert.NotEmpty(queuedEvent.Data);
            Assert.True(queuedEvent.Data.All(datum =>
                datum.Metadata.Any(entry => entry.Key == MetadataKeys.AwsAccount && entry.Value == eventAccountId))
            );
        }

        [Fact]
        public void RecordAppliesMissingAccountId()
        {
            var accountId = Guid.NewGuid().ToString();
            _sut.Enable();
            _sut.SetAccountId(accountId);

            _sut.Record(_sampleTelemetryEvent);

            var queuedEvent = _eventQueue.First();
            Assert.NotEmpty(queuedEvent.Data);
            Assert.True(queuedEvent.Data.All(datum =>
                datum.Metadata.Any(entry => entry.Key == MetadataKeys.AwsAccount && entry.Value == accountId))
            );
        }

        [Fact]
        public void RecordDoesNotApplyEmptyAccountId()
        {
            _sut.Enable();
            _sut.SetAccountId(string.Empty);

            _sut.Record(_sampleTelemetryEvent);

            var queuedEvent = _eventQueue.First();
            Assert.NotEmpty(queuedEvent.Data);
            Assert.True(queuedEvent.Data.All(datum =>
                datum.Metadata.All(entry => entry.Key != MetadataKeys.AwsAccount))
            );
        }

        [Fact]
        public async Task SendFeedback()
        {
            _sut.Initialize(_telemetryClient.Object, _telemetryPublisher.Object);

            await _sut.SendFeedback(Sentiment.Positive, "");

            _telemetryPublisher.Verify(mock => mock.SendFeedback(Sentiment.Positive, "", null), Times.Once);
        }

        [Fact]
        public async Task SendFeedback_Throws()
        {
            await Assert.ThrowsAsync<Exception>(() => _sut.SendFeedback(Sentiment.Positive, ""));

            _telemetryPublisher.Verify(mock => mock.SendFeedback(Sentiment.Positive, "", null), Times.Never);
        }

        [Fact]
        public async Task SendFeedback_WithMetadata()
        {
            _sut.Initialize(_telemetryClient.Object, _telemetryPublisher.Object);
            var metadata = new Dictionary<string, string> { { "abc", "def" } };

            await _sut.SendFeedback(Sentiment.Positive, "", metadata);

            _telemetryPublisher.Verify(mock => mock.SendFeedback(Sentiment.Positive, "", metadata), Times.Once);
        }

        [Fact]
        public void Dispose()
        {
            _sut.Initialize(_telemetryClient.Object, _telemetryPublisher.Object);

            _sut.Dispose();

            _telemetryPublisher.Verify(mock => mock.Dispose(), Times.Once);
            _telemetryClient.Verify(mock => mock.Dispose(), Times.Once);
        }
    }
}
