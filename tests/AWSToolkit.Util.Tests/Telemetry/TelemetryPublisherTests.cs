using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry.Internal;
using Amazon.Runtime;
using Amazon.ToolkitTelemetry;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Telemetry
{
    public class TelemetryPublisherTests : IDisposable
    {
        private readonly Guid _clientId = Guid.NewGuid();
        private readonly ConcurrentQueue<Metrics> _eventQueue = new ConcurrentQueue<Metrics>();
        private readonly Mock<TimeProvider> _timeProvider = new Mock<TimeProvider>();
        private DateTime _currentTime = DateTime.Now;
        private readonly Mock<ITelemetryClient> _telemetryClient = new Mock<ITelemetryClient>();

        private int _timesPublished = 0;
        private int _currentProcessorLoop = 0;
        private readonly Dictionary<int, Action> _beforeProcessorLoopActions = new Dictionary<int, Action>();
        private readonly Dictionary<int, Action> _processorLoopActions = new Dictionary<int, Action>();

        private readonly TelemetryPublisher _sut;

        public TelemetryPublisherTests()
        {
            _timeProvider.Setup(mock => mock.GetCurrentTime()).Returns(() => _currentTime);

            // The Processor sleeps after each processing loop.
            // Hijack this to perform atomic operations during specific iterations.
            _timeProvider.Setup(mock => mock.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<int, CancellationToken>((delayMs, token) =>
                {
                    if (_beforeProcessorLoopActions.TryGetValue(_currentProcessorLoop, out var action))
                    {
                        action();
                    }
                })
                .Returns<int, CancellationToken>((delayMs, token) =>
                {
                    var hasAction = _processorLoopActions.TryGetValue(_currentProcessorLoop, out var action);
                    _currentProcessorLoop++;

                    if (hasAction)
                    {
                        action();
                    }

                    return Task.CompletedTask;
                });

            _sut = new TelemetryPublisher(_eventQueue, _clientId, _timeProvider.Object);
            _sut.IsTelemetryEnabled = true;
            _sut.MetricsPublished += (sender, args) => { _timesPublished++; };
        }

        [Fact(Timeout = 1000)]
        public async Task PublishOneEvent()
        {
            BeforeProcessorLoop(0, () => AddEventsToQueue(1));
            _sut.Initialize(_telemetryClient.Object);

            await WaitForTimesPublished(1);

            Assert.Empty(_eventQueue);
            VerifyPostMetricsCalls(1, Times.Once());
        }

        [Fact(Timeout = 1000)]
        public async Task PublishesInBatches()
        {
            // Populate with more than one batch worth of events
            BeforeProcessorLoop(0, () => AddEventsToQueue(TelemetryPublisher.MAX_BATCH_SIZE + 1));
            BeforeProcessorLoop(1, () => Assert.Empty(_eventQueue));

            _sut.Initialize(_telemetryClient.Object);

            await WaitForTimesPublished(1);

            Assert.Empty(_eventQueue);
            VerifyPostMetricsTimesCalled(Times.AtLeast(2));
        }

        [Fact(Timeout = 2000)]
        public async Task Publish4xxFailuresDoNotReturnToQueue()
        {
            const int elementCount = TelemetryPublisher.QUEUE_SIZE_THRESHOLD;
            BeforeProcessorLoop(0, () => AddEventsToQueue(elementCount));

            _telemetryClient.Setup(mock => mock.PostMetrics(
                _clientId,
                It.IsAny<IList<Metrics>>(),
                It.IsAny<CancellationToken>()
            )).Throws(new AmazonToolkitTelemetryException("Simulating Http 4xx level error", ErrorType.Unknown, "", "",
                HttpStatusCode.BadRequest));

            _sut.Initialize(_telemetryClient.Object);

            await WaitForTimesPublished(1);

            Assert.Empty(_eventQueue);
            VerifyPostMetricsCalls(elementCount, Times.Once());
        }

        [Fact(Timeout = 1000)]
        public async Task Publish5xxFailuresReturnToQueue()
        {
            const int elementCount = TelemetryPublisher.QUEUE_SIZE_THRESHOLD;
            BeforeProcessorLoop(0, () => AddEventsToQueue(elementCount));
            OnProcessorLoop(1,
                () => throw new Exception("Kill processing loop to stop the queue from being re-processed."));

            _telemetryClient.Setup(mock => mock.PostMetrics(
                _clientId,
                It.IsAny<IList<Metrics>>(),
                It.IsAny<CancellationToken>()
            )).Throws(new AmazonToolkitTelemetryException("Simulating Http 5xx level error", ErrorType.Unknown, "", "",
                HttpStatusCode.InternalServerError));

            _sut.Initialize(_telemetryClient.Object);

            await WaitForTimesPublished(1);

            Assert.Equal(elementCount, _eventQueue.Count);
            // 5xx errors stop the publish loop
            VerifyPostMetricsCalls(elementCount, Times.Once());
        }

        /// <summary>
        /// Tests Non-http related exceptions (example: offline)
        /// </summary>
        [Fact(Timeout = 1000)]
        public async Task FailedBatchesReturnToQueue()
        {
            // const int elementCount = TelemetryPublisher.QUEUE_SIZE_THRESHOLD;
            const int elementCount = 3;
            BeforeProcessorLoop(0, () => AddEventsToQueue(elementCount));
            OnProcessorLoop(1,
                () => throw new Exception("Kill processing loop to stop the queue from being re-processed."));

            _telemetryClient.Setup(mock => mock.PostMetrics(
                _clientId,
                It.IsAny<IList<Metrics>>(),
                It.IsAny<CancellationToken>()
            )).Throws(new Exception("Simulating service call failure"));

            _sut.Initialize(_telemetryClient.Object);

            await WaitForTimesPublished(1);

            // If the Publisher infinite looped, we would never get a Metrics Published event (never get here)
            Assert.Equal(elementCount, _eventQueue.Count);
            VerifyPostMetricsCalls(elementCount, Times.AtLeast(1));
        }

        [Fact(Timeout = 1000)]
        public void DoesNotPublishWhenDisabled()
        {
            BeforeProcessorLoop(0, () => AddEventsToQueue(1));

            // Make test explode if publish is attempted
            _telemetryClient.Setup(mock => mock.PostMetrics(
                _clientId,
                It.IsAny<IList<Metrics>>(),
                It.IsAny<CancellationToken>()
            )).Throws(new Exception("Publish should never happen"));

            _sut.IsTelemetryEnabled = false;
            _sut.Initialize(_telemetryClient.Object);

            // Let a couple of publish loops occur, queue should not get emptied
            while (_currentProcessorLoop < 5)
            {
            }

            Assert.Single(_eventQueue);
            VerifyPostMetricsTimesCalled(Times.Never());
        }

        [Fact(Timeout = 1000)]
        public async Task PublishIfSizeThresholdExceeded()
        {
            // The first loop will publish because there wasn't a prior publish (uses the time-based check)
            BeforeProcessorLoop(0, () => AddEventsToQueue(1));
            // Queue one and verify it does not publish (under size threshold)
            BeforeProcessorLoop(1, () =>
            {
                Assert.Empty(_eventQueue);
                AddEventsToQueue(1);
            });
            // Cross the size threshold, and expect a publish to occur
            BeforeProcessorLoop(2, () =>
            {
                Assert.NotEmpty(_eventQueue); // should be not empty, checking that this fails test
                AddEventsToQueue(TelemetryPublisher.QUEUE_SIZE_THRESHOLD - 1);
            });

            _sut.Initialize(_telemetryClient.Object);

            await WaitForTimesPublished(2);

            Assert.Empty(_eventQueue);
            VerifyPostMetricsCalls(1, Times.Once());
            VerifyPostMetricsCalls(TelemetryPublisher.QUEUE_SIZE_THRESHOLD, Times.Once());
        }

        [Fact(Timeout = 1000)]
        public async Task PublishIfTimeThresholdExceeded()
        {
            var startTime = DateTime.Now;
            _currentTime = startTime;

            // The first loop will publish because there wasn't a prior publish (uses the time-based check)
            BeforeProcessorLoop(0, () => AddEventsToQueue(1));
            BeforeProcessorLoop(1, () =>
            {
                Assert.Empty(_eventQueue);
                AddEventsToQueue(1);
            });
            BeforeProcessorLoop(2, () =>
            {
                // Verify the last queued item did not publish yet
                Assert.NotEmpty(_eventQueue);
            });

            _sut.Initialize(_telemetryClient.Object);

            await WaitForTimesPublished(1);

            // Check that under the time threshold does not publish
            _currentTime = startTime + new TimeSpan(0, 0, 1);
            while (_currentProcessorLoop < 2)
            {
            }

            // Cross the threshold, expect to publish
            _currentTime = startTime + TelemetryPublisher.MAX_PUBLISH_INTERVAL;

            await WaitForTimesPublished(2);

            Assert.Empty(_eventQueue);
            VerifyPostMetricsCalls(1, Times.Exactly(2));
        }

        public void Dispose()
        {
            _sut.Dispose();
        }

        private void BeforeProcessorLoop(int iteration, Action action)
        {
            _beforeProcessorLoopActions[iteration] = action;
        }

        private void OnProcessorLoop(int iteration, Action action)
        {
            _processorLoopActions[iteration] = action;
        }

        private void AddEventsToQueue(int quantity)
        {
            TestHelper.AddEventsToQueue(_eventQueue, quantity);
        }

        private async Task WaitForTimesPublished(int timesCalled)
        {
            while (_timesPublished < timesCalled)
            {
                await Task.Delay(1);
            }
        }

        /// <summary>
        /// Verifies that the telemetry client's "PostMetrics" method was called with
        /// a specific number of events, a specific number of times.
        /// </summary>
        private void VerifyPostMetricsCalls(int expectedEventCount, Times expectedCallCount)
        {
            _telemetryClient.Verify(mock => mock.PostMetrics(
                    _clientId,
                    It.Is<IList<Metrics>>(ExpectedCount(expectedEventCount)),
                    It.IsAny<CancellationToken>()
                ),
                expectedCallCount
            );
        }

        /// <summary>
        /// Verifies that the telemetry client's "PostMetrics" method was called a specific
        /// number of times.
        /// </summary>
        private void VerifyPostMetricsTimesCalled(Times expectedCallCount)
        {
            _telemetryClient.Verify(mock => mock.PostMetrics(
                    _clientId,
                    It.IsAny<IList<Metrics>>(),
                    It.IsAny<CancellationToken>()
                ),
                expectedCallCount
            );
        }

        private static Expression<Func<IList<Metrics>, bool>> ExpectedCount(int size)
        {
            return list => list.Count == size;
        }
    }
}