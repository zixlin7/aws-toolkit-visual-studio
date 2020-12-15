using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Telemetry.Internal;
using Amazon.Runtime;
using Amazon.ToolkitTelemetry;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        private object _syncObj = new object();
        private readonly List<int> _queueSizeBeforeProcessorLoop = new List<int>();

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

                    lock (_syncObj)
                    {
                        _queueSizeBeforeProcessorLoop.Add(_eventQueue.Count);
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

        [Fact(Timeout = 2000)]
        public async Task PublishesInBatches()
        {
            // Populate with more than one batch worth of events
            var testEventCount = TelemetryPublisher.MAX_BATCH_SIZE + 1;
            BeforeProcessorLoop(0, () => AddEventsToQueue(testEventCount));

            _sut.Initialize(_telemetryClient.Object);

            await WaitForTimesPublished(1);

            lock (_syncObj)
            {
                // There should have only ever been a queue size of
                // testEventCount or empty.
                Assert.Empty(_queueSizeBeforeProcessorLoop
                    .ToArray()
                    .Where(size => size > 0 && size != testEventCount));
            }

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

        [Fact(Timeout = 2000)]
        public async Task PublishIfSizeThresholdExceeded()
        {
            // The first loop will publish because there wasn't a prior publish (uses the time-based check)
            BeforeProcessorLoop(0, () => AddEventsToQueue(1));
            // Wait a couple of loops to ensure the first event was published
            // Queue one with the expectation it remains in the queue and isn't published (under size threshold)
            BeforeProcessorLoop(5, () => AddEventsToQueue(1));
            // Wait a couple of loops, then meet the size threshold, and expect a publish to occur
            BeforeProcessorLoop(10, () => AddEventsToQueue(TelemetryPublisher.QUEUE_SIZE_THRESHOLD - 1));

            _sut.Initialize(_telemetryClient.Object);

            await WaitForTimesPublished(2);

            // Nothing should be left to publish
            Assert.Empty(_eventQueue);

            // One metric should have been published, then QUEUE_SIZE_THRESHOLD metrics should have been published
            VerifyPostMetricsCalls(1, Times.Once());
            VerifyPostMetricsCalls(TelemetryPublisher.QUEUE_SIZE_THRESHOLD, Times.Once());

            // The queue size should have started with one, then emptied out, then had one, then QUEUE_SIZE_THRESHOLD
            var expectedQueueSizes = new List<int>() { 1, 0, 1, TelemetryPublisher.QUEUE_SIZE_THRESHOLD };
            IList<int> actualQueueSizes = null;

            lock (_syncObj)
            {
                actualQueueSizes = RemoveAdjacentDuplicates(_queueSizeBeforeProcessorLoop.Take(20).ToArray());
                Assert.Single(_queueSizeBeforeProcessorLoop.Where(size => size == TelemetryPublisher.QUEUE_SIZE_THRESHOLD));
            }

            Assert.Equal(
                expectedQueueSizes,
                actualQueueSizes.Take(expectedQueueSizes.Count));
        }

        [Fact(Timeout = 2000)]
        public async Task PublishIfTimeThresholdExceeded()
        {
            var startTime = DateTime.Now;
            _currentTime = startTime;

            // The first loop will publish because there wasn't a prior publish (uses the time-based check)
            BeforeProcessorLoop(0, () => AddEventsToQueue(1));
            // Wait a couple of loops to ensure the first event was published
            // Queue one with the expectation it remains in the queue and isn't published (under size threshold)
            BeforeProcessorLoop(5, () => AddEventsToQueue(1));

            _sut.Initialize(_telemetryClient.Object);

            await WaitForTimesPublished(1);

            // Allow some processor loops to pass
            _currentTime = startTime + new TimeSpan(0, 0, 1);
            while (_currentProcessorLoop < 10)
            {
            }

            // Check that there weren't additional publish operations
            Assert.Equal(1, _timesPublished);

            // Cross the time threshold, expect to publish
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

        /// <summary>
        /// In: 1 1 1 2 2 1 3 3 3
        /// Out: 1 2 1 3
        /// </summary>
        private static IList<int> RemoveAdjacentDuplicates(int[] list)
        {
            var result = new List<int>();

            if (list == null || !list.Any())
            {
                return result;
            }

            var lastValue = list.First() + 1;

            foreach (var i in list)
            {
                if (i != lastValue)
                {
                    result.Add(i);
                }

                lastValue = i;
            }

            return result;
        }
    }
}
