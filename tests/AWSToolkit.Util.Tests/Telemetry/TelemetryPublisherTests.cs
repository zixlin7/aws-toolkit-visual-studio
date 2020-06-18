using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Telemetry.Internal;
using Amazon.Runtime;
using Amazon.ToolkitTelemetry;
using Moq;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Telemetry
{
    public class TelemetryPublisherTests
    {
        private readonly Guid _clientId = Guid.NewGuid();
        private readonly ConcurrentQueue<TelemetryEvent> _eventQueue = new ConcurrentQueue<TelemetryEvent>();
        private readonly Mock<TimeProvider> _timeProvider = new Mock<TimeProvider>();
        private DateTime _currentTime = DateTime.Now;
        private readonly Mock<ITelemetryClient> _telemetryClient = new Mock<ITelemetryClient>();

        private readonly TelemetryPublisher _sut;
        private readonly AutoResetEvent _publisherMetricsPublishedEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _publisherIntervalSkippedEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _taskDelayEvent = new AutoResetEvent(false);

        public TelemetryPublisherTests()
        {
            _timeProvider.Setup(mock => mock.GetCurrentTime()).Returns(() => _currentTime);
            _timeProvider.Setup(mock => mock.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns<int, CancellationToken>((delayMs, token) =>
                {
                    // Tests advance the Delay by calling AdvancePublisherOuterLoop
                    _taskDelayEvent.WaitOne();
                    return Task.CompletedTask;
                });

            _sut = new TelemetryPublisher(_eventQueue, _clientId, _timeProvider.Object);
            _sut.IsTelemetryEnabled = true;
            _sut.MetricsPublished += (sender, args) => { _publisherMetricsPublishedEvent.Set(); };
            _sut.PublishIntervalSkipped += (sender, args) => { _publisherIntervalSkippedEvent.Set(); };
        }

        [Fact]
        public void PublishOneEvent()
        {
            _sut.Initialize(_telemetryClient.Object);
            AddEventsToQueue(1);

            AdvancePublisherOuterLoop();

            WaitForMetricsPublishedEvent();
            Assert.Empty(_eventQueue);
            VerifyPostMetricsCalls(1, Times.Once());
        }

        [Fact]
        public void PublishesInBatches()
        {
            _sut.Initialize(_telemetryClient.Object);

            // Ensure the Publisher's outer loop has started
            AdvancePublisherOuterLoop();
            WaitForPublishIntervalSkippedEvent();

            // Populate with more than one batch worth of events
            AddEventsToQueue(TelemetryPublisher.MAX_BATCH_SIZE + 1);
            AdvancePublisherOuterLoop();
            WaitForMetricsPublishedEvent();

            Assert.Empty(_eventQueue);

            _telemetryClient.Verify(mock => mock.PostMetrics(
                    _clientId,
                    It.IsAny<IList<TelemetryEvent>>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Exactly(2)
            );
        }

        [Fact]
        public void Publish4xxFailuresDoNotReturnToQueue()
        {
            const int elementCount = TelemetryPublisher.QUEUE_SIZE_THRESHOLD;

            _telemetryClient.Setup(mock => mock.PostMetrics(
                _clientId,
                It.IsAny<IList<TelemetryEvent>>(),
                It.IsAny<CancellationToken>()
            )).Throws(new AmazonToolkitTelemetryException("Simulating Http 4xx level error", ErrorType.Unknown, "", "",
                HttpStatusCode.BadRequest));

            _sut.Initialize(_telemetryClient.Object);
            AddEventsToQueue(elementCount);

            AdvancePublisherOuterLoop();

            WaitForMetricsPublishedEvent();
            Assert.Empty(_eventQueue);
            VerifyPostMetricsCalls(elementCount, Times.Once());
        }

        [Fact]
        public void Publish5xxFailuresReturnToQueue()
        {
            const int elementCount = TelemetryPublisher.QUEUE_SIZE_THRESHOLD;

            _telemetryClient.Setup(mock => mock.PostMetrics(
                _clientId,
                It.IsAny<IList<TelemetryEvent>>(),
                It.IsAny<CancellationToken>()
            )).Throws(new AmazonToolkitTelemetryException("Simulating Http 5xx level error", ErrorType.Unknown, "", "",
                HttpStatusCode.InternalServerError));

            _sut.Initialize(_telemetryClient.Object);

            // Ensure the Publisher's outer loop has started
            AdvancePublisherOuterLoop();
            WaitForPublishIntervalSkippedEvent();

            AddEventsToQueue(elementCount);
            AdvancePublisherOuterLoop();
            WaitForMetricsPublishedEvent();

            Assert.Equal(elementCount, _eventQueue.Count);
            // 5xx errors stop the publish loop
            VerifyPostMetricsCalls(elementCount, Times.Once());
        }

        /// <summary>
        /// Tests Non-http related exceptions (example: offline)
        /// </summary>
        [Fact]
        public void FailedBatchesReturnToQueue()
        {
            const int elementCount = TelemetryPublisher.QUEUE_SIZE_THRESHOLD;

            _telemetryClient.Setup(mock => mock.PostMetrics(
                _clientId,
                It.IsAny<IList<TelemetryEvent>>(),
                It.IsAny<CancellationToken>()
            )).Throws(new Exception("Simulating service call failure"));

            _sut.Initialize(_telemetryClient.Object);

            // Ensure the Publisher's outer loop has started
            AdvancePublisherOuterLoop();
            WaitForPublishIntervalSkippedEvent();

            AddEventsToQueue(elementCount);
            AdvancePublisherOuterLoop();
            WaitForMetricsPublishedEvent();

            Assert.Equal(elementCount, _eventQueue.Count);
            VerifyPostMetricsCalls(TelemetryPublisher.QUEUE_SIZE_THRESHOLD, Times.AtLeast(1));
        }

        [Fact]
        public void ReQueuedBatchesDoNotInfiniteLoop()
        {
            const int elementCount = TelemetryPublisher.MAX_BATCH_SIZE + 1;

            _telemetryClient.Setup(mock => mock.PostMetrics(
                _clientId,
                It.IsAny<IList<TelemetryEvent>>(),
                It.IsAny<CancellationToken>()
            )).Throws(new Exception("Simulating service call failure"));

            _sut.Initialize(_telemetryClient.Object);

            AdvancePublisherOuterLoop();
            WaitForPublishIntervalSkippedEvent();

            AddEventsToQueue(elementCount);
            AdvancePublisherOuterLoop();

            // If the Publisher infinite looped, we would never get a Metrics Published event
            WaitForMetricsPublishedEvent();

            Assert.Equal(elementCount, _eventQueue.Count);
            // The first PostMetrics call gets 20 items
            // The second call gets 1 item + 19 of the items from batch 1 that were re-queued, and the 
            // loop is expected to stop afterwards.
            VerifyPostMetricsCalls(TelemetryPublisher.MAX_BATCH_SIZE, Times.Exactly(2));
        }

        [Fact]
        public void DoesNotPublishWhenDisabled()
        {
            // Make test explode if publish is attempted
            _telemetryClient.Setup(mock => mock.PostMetrics(
                _clientId,
                It.IsAny<IList<TelemetryEvent>>(),
                It.IsAny<CancellationToken>()
            )).Throws(new Exception("Publish should never happen"));

            _sut.IsTelemetryEnabled = false;
            _sut.Initialize(_telemetryClient.Object);
            AddEventsToQueue(1);

            AdvancePublisherOuterLoop();

            WaitForPublishIntervalSkippedEvent();
            Assert.Single(_eventQueue);

            _telemetryClient.Verify(mock => mock.PostMetrics(
                    _clientId,
                    It.IsAny<IList<TelemetryEvent>>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never
            );
        }

        [Fact]
        public void PublishIfSizeThresholdExceeded()
        {
            _sut.Initialize(_telemetryClient.Object);

            // Ensure the Publisher's outer loop has started
            AdvancePublisherOuterLoop();
            WaitForPublishIntervalSkippedEvent();

            // The first loop will publish because there wasn't a prior publish (uses the time-based check)
            AddEventsToQueue(1);
            AdvancePublisherOuterLoop();
            WaitForMetricsPublishedEvent();

            // Queue one and verify it does not publish (under size threshold)
            AddEventsToQueue(1);
            AdvancePublisherOuterLoop();
            WaitForPublishIntervalSkippedEvent();

            // Cross the size threshold, expect to publish
            AddEventsToQueue(TelemetryPublisher.QUEUE_SIZE_THRESHOLD - 1);
            AdvancePublisherOuterLoop();
            WaitForMetricsPublishedEvent();

            Assert.Empty(_eventQueue);
            VerifyPostMetricsCalls(TelemetryPublisher.QUEUE_SIZE_THRESHOLD, Times.Once());
        }

        [Fact]
        public void PublishIfTimeThresholdExceeded()
        {
            var startTime = DateTime.Now;
            _currentTime = startTime;

            _sut.Initialize(_telemetryClient.Object);

            // Ensure the Publisher's outer loop has started
            AdvancePublisherOuterLoop();
            WaitForPublishIntervalSkippedEvent();

            // The first loop will publish because there wasn't a prior publish (uses the time-based check)
            AddEventsToQueue(1);
            AdvancePublisherOuterLoop();
            WaitForMetricsPublishedEvent();

            // Check that under the time threshold does not publish
            _currentTime = startTime + new TimeSpan(0, 0, 1);
            AddEventsToQueue(2);
            AdvancePublisherOuterLoop();
            WaitForPublishIntervalSkippedEvent();

            // Cross the threshold, expect to publish
            _currentTime = startTime + TelemetryPublisher.MAX_PUBLISH_INTERVAL;
            AdvancePublisherOuterLoop();
            WaitForMetricsPublishedEvent();

            Assert.Empty(_eventQueue);
            VerifyPostMetricsCalls(2, Times.Once());
        }

        [Fact]
        public void DisposeTest()
        {
            _sut.Initialize(_telemetryClient.Object);

            // Make sure the loop is up and running
            AdvancePublisherOuterLoop();
            WaitForPublishIntervalSkippedEvent();

            // Now when we dispose, the cancellation token should end the outer loop
            _timeProvider.Setup(mock => mock.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns<int, CancellationToken>((delayMs, token) =>
                {
                    // Disposing will cause the cancellation token to trigger
                    Assert.Throws<TaskCanceledException>(() => { _taskDelayEvent.WaitOne(); });
                    return Task.CompletedTask;
                });

            _sut.Dispose();
            _timeProvider.Verify(mock => mock.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        private void WaitForMetricsPublishedEvent()
        {
            var timeout = Debugger.IsAttached ? -1 : 5000;
            Assert.True(_publisherMetricsPublishedEvent.WaitOne(timeout), "MetricsPublished wasn't fired within expected time frame");
        }

        private void WaitForPublishIntervalSkippedEvent()
        {
            var timeout = Debugger.IsAttached ? -1 : 2000;
            Assert.True(_publisherIntervalSkippedEvent.WaitOne(timeout), "PublishIntervalSkipped wasn't fired within expected time frame");
        }

        private void AdvancePublisherOuterLoop()
        {
            _taskDelayEvent.Set();
        }

        private void AddEventsToQueue(int quantity)
        {
            TestHelper.AddEventsToQueue(_eventQueue, quantity);
        }

        /// <summary>
        /// Verifies that the telemetry client's "PostMetrics" method was called with
        /// a specific number of events, a specific number of times.
        /// </summary>
        private void VerifyPostMetricsCalls(int expectedEventCount, Times expectedCallCount)
        {
            _telemetryClient.Verify(mock => mock.PostMetrics(
                    _clientId,
                    It.Is<IList<TelemetryEvent>>(ExpectedCount(expectedEventCount)),
                    It.IsAny<CancellationToken>()
                ),
                expectedCallCount
            );
        }

        private static Expression<Func<IList<TelemetryEvent>, bool>> ExpectedCount(int size)
        {
            return list => list.Count == size;
        }
    }
}