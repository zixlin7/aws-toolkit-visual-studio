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

using Amazon.AWSToolkit.Telemetry;

using Xunit;
using Xunit.Abstractions;

using Sentiment = Amazon.AwsToolkit.Telemetry.Events.Core.Sentiment;

namespace Amazon.AWSToolkit.Util.Tests.Telemetry
{
    public class TelemetryPublisherTests : IDisposable
    {
        private const int WaitForEventMs = 1500;

        private readonly ITestOutputHelper _testOutput;

        private readonly ClientId _clientId = ClientId.AutomatedTestClientId;
        private readonly ConcurrentQueue<Metrics> _eventQueue = new ConcurrentQueue<Metrics>();
        private readonly Mock<TimeProvider> _timeProvider = new Mock<TimeProvider>();
        private DateTime _currentTime = DateTime.Now;
        private readonly Mock<ITelemetryClient> _telemetryClient = new Mock<ITelemetryClient>();

        private int _timesPublished = 0;
        private int _currentProcessorLoop = 0;
        private readonly Dictionary<int, Action> _beforeProcessorLoopActions = new Dictionary<int, Action>();
        private readonly Dictionary<int, Action> _processorLoopActions = new Dictionary<int, Action>();

        private readonly object _processorLoopSyncObj = new object();
        private readonly ManualResetEvent _processorLoopThresholdEvent = new ManualResetEvent(false);
        private int _processorLoopThreshold = int.MaxValue;

        private readonly object _timesPublishedSyncObj = new object();
        private readonly ManualResetEvent _timesPublishedThresholdEvent = new ManualResetEvent(false);
        private int _timesPublishedThreshold = int.MaxValue;

        private readonly object _syncObj = new object();
        private readonly List<int> _queueSizeBeforeProcessorLoop = new List<int>();

        private readonly TelemetryPublisher _sut;

        public TelemetryPublisherTests(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;

            _timeProvider.Setup(mock => mock.GetCurrentTime()).Returns(() => _currentTime);

            // The Processor sleeps after each processing loop.
            // Hijack this to perform atomic operations during specific iterations.
            _timeProvider.Setup(mock => mock.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<int, CancellationToken>((delayMs, token) =>
                {
                    _testOutput.WriteLine($"Starting processor loop {_currentProcessorLoop}");

                    UpdateTimesPublishedEvent();

                    if (_beforeProcessorLoopActions.TryGetValue(_currentProcessorLoop, out var action))
                    {
                        _testOutput.WriteLine($"Running an action before processor loop {_currentProcessorLoop}");
                        action();
                    }

                    lock (_syncObj)
                    {
                        _testOutput.WriteLine($"Queue size: {_eventQueue.Count}");
                        _queueSizeBeforeProcessorLoop.Add(_eventQueue.Count);
                    }
                })
                .Returns<int, CancellationToken>((delayMs, token) =>
                {
                    _testOutput.WriteLine($"Completing processor loop {_currentProcessorLoop}");
                    var hasAction = _processorLoopActions.TryGetValue(_currentProcessorLoop, out var action);
                    _currentProcessorLoop++;
                    UpdateProcessorLoopsEvent();

                    if (hasAction)
                    {
                        _testOutput.WriteLine($"Running an action after processor loop {_currentProcessorLoop}");
                        action();
                    }

                    return Task.CompletedTask;
                });

            _sut = new TelemetryPublisher(_eventQueue, _clientId, _timeProvider.Object);
            _sut.IsTelemetryEnabled = true;
            _sut.MetricsPublished += (sender, args) =>
            {
                _timesPublished++;
                _testOutput.WriteLine($"Metrics now published {_timesPublished} times");
                UpdateTimesPublishedEvent();
            };
        }

        [Fact(Timeout = 1000)]
        public void PublishOneEvent()
        {
            BeforeProcessorLoop(0, () => AddEventsToQueue(1));
            _sut.Initialize(_telemetryClient.Object);

            WaitForTimesPublished(1);

            Assert.Empty(_eventQueue);
            VerifyPostMetricsCalls(1, Times.Once());
        }

        [Fact(Timeout = 2000)]
        public void PublishesInBatches()
        {
            // Populate with more than one batch worth of events
            var testEventCount = TelemetryPublisher.MAX_BATCH_SIZE + 1;
            BeforeProcessorLoop(0, () => AddEventsToQueue(testEventCount));

            _sut.Initialize(_telemetryClient.Object);

            WaitForTimesPublished(1);

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
        public void Publish4xxFailuresDoNotReturnToQueue()
        {
            const int elementCount = TelemetryPublisher.QUEUE_SIZE_THRESHOLD;
            BeforeProcessorLoop(0, () => AddEventsToQueue(elementCount));

            _telemetryClient.Setup(mock => mock.PostMetrics(
                _clientId,
                It.IsAny<IList<Metrics>>(),
                It.IsAny<CancellationToken>()
            )).Callback(() =>
            {
                _testOutput.WriteLine("Telemetry PostMetrics was called, simulating a 4xx throw.");
            }).Throws(new AmazonToolkitTelemetryException("Simulating Http 4xx level error", ErrorType.Unknown, "", "",
                HttpStatusCode.BadRequest));

            _testOutput.WriteLine("Initializing the Telemetry Publisher");
            _sut.Initialize(_telemetryClient.Object);

            WaitForTimesPublished(1);

            Assert.Empty(_eventQueue);
            VerifyPostMetricsCalls(elementCount, Times.Once());
        }

        [Fact(Timeout = 1000)]
        public void Publish5xxFailuresReturnToQueue()
        {
            ManualResetEvent afterPublishEvent = new ManualResetEvent(false);
            int queueCountAfterPublish = -1;

            const int elementCount = TelemetryPublisher.QUEUE_SIZE_THRESHOLD;
            BeforeProcessorLoop(0, () => AddEventsToQueue(elementCount));
            OnProcessorLoop(1,
                () =>
                {
                    // Drain the queue so that iterative processor loops don't process
                    // them. Used to verify PostMetrics call.
                    queueCountAfterPublish = ClearEventQueue().Count;
                    afterPublishEvent.Set();
                });

            _telemetryClient.Setup(mock => mock.PostMetrics(
                _clientId,
                It.IsAny<IList<Metrics>>(),
                It.IsAny<CancellationToken>()
            )).Callback(() =>
            {
                _testOutput.WriteLine("Telemetry PostMetrics was called, simulating a 5xx throw.");
            }).Throws(new AmazonToolkitTelemetryException("Simulating Http 5xx level error", ErrorType.Unknown, "", "",
                HttpStatusCode.InternalServerError));

            _testOutput.WriteLine("Initializing the Telemetry Publisher");
            _sut.Initialize(_telemetryClient.Object);

            WaitForTimesPublished(1);

            _testOutput.WriteLine("Waiting for post processor loop to complete");
            afterPublishEvent.WaitOne(WaitForEventMs);

            _testOutput.WriteLine("Verifying state");
            Assert.Equal(elementCount, queueCountAfterPublish);
            // 5xx errors stop the publish loop
            VerifyPostMetricsCalls(elementCount, Times.Once());
        }

        /// <summary>
        /// Tests Non-http related exceptions (example: offline)
        /// </summary>
        [Fact(Timeout = 2000)]
        public void FailedBatchesReturnToQueue()
        {
            const int elementCount = 3;
            BeforeProcessorLoop(0, () => AddEventsToQueue(elementCount));

            int queuedElementsAfterPublish = 0;
            ManualResetEvent afterPublishEvent = new ManualResetEvent(false);

            // Measure the state after the first metrics publish takes place
            void OneTimeMetricsPublishedHandler(object sender, EventArgs args)
            {
                _testOutput.WriteLine("After Metrics Published handler");
                _sut.MetricsPublished -= OneTimeMetricsPublishedHandler;

                // See how many items are in the queue after the publish event
                // Clear the queue so the processor loop does not continue to try publishing them.
                queuedElementsAfterPublish = ClearEventQueue().Count;

                afterPublishEvent.Set();
            }

            _sut.MetricsPublished += OneTimeMetricsPublishedHandler;

            _telemetryClient.Setup(mock => mock.PostMetrics(
                    _clientId,
                    It.IsAny<IList<Metrics>>(),
                    It.IsAny<CancellationToken>()
                ))
                .Callback(() => _testOutput.WriteLine("Simulating a failure in the PostMetrics call"))
                .Throws(new Exception("Simulating service call failure"));

            _testOutput.WriteLine("Initializing the Telemetry Publisher");
            _sut.Initialize(_telemetryClient.Object);

            _testOutput.WriteLine("Waiting for post processor loop to complete");
            afterPublishEvent.WaitOne(WaitForEventMs);

            Assert.Equal(elementCount, queuedElementsAfterPublish);

            // PostMetrics is called twice due to metrics-resend detection
            VerifyPostMetricsCalls(elementCount, Times.Exactly(2));
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

            // allow some publish loops to take place before verifying nothing was transmitted
            WaitForProcessorLoopThreshold(5);

            Assert.Single(_eventQueue);
            VerifyPostMetricsTimesCalled(Times.Never());
        }

        [Fact(Timeout = 2000)]
        public void PublishIfSizeThresholdExceeded()
        {
            // The first loop will publish because there wasn't a prior publish (uses the time-based check)
            BeforeProcessorLoop(0, () => AddEventsToQueue(1));
            // Wait a couple of loops to ensure the first event was published
            // Queue one with the expectation it remains in the queue and isn't published (under size threshold)
            BeforeProcessorLoop(5, () => AddEventsToQueue(1));
            // Wait a couple of loops, then meet the size threshold, and expect a publish to occur
            BeforeProcessorLoop(10, () => AddEventsToQueue(TelemetryPublisher.QUEUE_SIZE_THRESHOLD - 1));

            _sut.Initialize(_telemetryClient.Object);

            WaitForTimesPublished(2);

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
        public void PublishIfTimeThresholdExceeded()
        {
            var startTime = DateTime.Now;
            _currentTime = startTime;

            // The first loop will publish because there wasn't a prior publish (uses the time-based check)
            BeforeProcessorLoop(0, () => AddEventsToQueue(1));
            // Wait a couple of loops to ensure the first event was published
            // Queue one with the expectation it remains in the queue and isn't published (under size threshold)
            BeforeProcessorLoop(5, () => AddEventsToQueue(1));

            _sut.Initialize(_telemetryClient.Object);

            WaitForTimesPublished(1);

            // Allow some processor loops to pass
            _currentTime = startTime + new TimeSpan(0, 0, 1);

            // allow some publish loops to take place before verifying
            WaitForProcessorLoopThreshold(10);

            // Check that there weren't additional publish operations
            Assert.Equal(1, _timesPublished);

            // Cross the time threshold, expect to publish
            _currentTime = startTime + TelemetryPublisher.MAX_PUBLISH_INTERVAL;

            WaitForTimesPublished(2);

            Assert.Empty(_eventQueue);
            VerifyPostMetricsCalls(1, Times.Exactly(2));
        }

        [Fact]
        public async Task SendFeedback()
        {
            _sut.Initialize(_telemetryClient.Object);
           await _sut.SendFeedback(Sentiment.Positive, "", null);

           _telemetryClient.Verify(mock => mock.SendFeedback(Sentiment.Positive, "", null), Times.Once);
        }

        [Fact]
        public async Task SendFeedback_Throws()
        {
            await Assert.ThrowsAsync<Exception>(() => _sut.SendFeedback(Sentiment.Positive, "", null));

            _telemetryClient.Verify(mock => mock.SendFeedback(Sentiment.Positive, "" , null), Times.Never);
        }

        [Fact]
        public async Task SendFeedback_WithMetadata()
        {
            _sut.Initialize(_telemetryClient.Object);
            var metadata = new Dictionary<string, string> { { "abc", "def" } };

            await _sut.SendFeedback(Sentiment.Positive, "", metadata);

            _telemetryClient.Verify(mock => mock.SendFeedback(Sentiment.Positive, "", metadata), Times.Once);
        }

        public void Dispose()
        {
            _sut.Dispose();
            _timesPublishedThresholdEvent.Dispose();
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
            _testOutput.WriteLine($"Adding {quantity} event(s) to queue");
            TestHelper.AddEventsToQueue(_eventQueue, quantity);
        }

        private void WaitForTimesPublished(int timesCalled)
        {
            lock (_timesPublishedSyncObj)
            {
                _testOutput.WriteLine($"Waiting for metrics to be published at least {timesCalled} time(s)");
                _timesPublishedThreshold = timesCalled;
                _timesPublishedThresholdEvent.Reset();
            }

            _timesPublishedThresholdEvent.WaitOne(WaitForEventMs);
            
            _testOutput.WriteLine($"Metrics were published at least {timesCalled} times (actual: {_timesPublished})");
        }

        private void WaitForProcessorLoopThreshold(int minimumProcessorLoop)
        {
            lock (_processorLoopSyncObj)
            {
                _testOutput.WriteLine($"Waiting for at least {minimumProcessorLoop} telemetry processor loop(s)");
                _processorLoopThreshold = minimumProcessorLoop;
                _processorLoopThresholdEvent.Reset();
            }

            _processorLoopThresholdEvent.WaitOne(WaitForEventMs);
            
            _testOutput.WriteLine($"At least {minimumProcessorLoop} processor loops have happened (actual: {_currentProcessorLoop})");
        }

        /// <summary>
        /// Clears the event queue, returning all of the popped items
        /// </summary>
        private IList<Metrics> ClearEventQueue()
        {
            var metrics = new List<Metrics>();

            while (_eventQueue.TryDequeue(out var metric))
            {
                metrics.Add(metric);
            }

            _testOutput.WriteLine($"Cleared {metrics.Count} items from the processor queue");
            return metrics;
        }

        private void UpdateTimesPublishedEvent()
        {
            lock (_timesPublishedSyncObj)
            {
                if (_timesPublished >= _timesPublishedThreshold)
                {
                    _testOutput.WriteLine("Times Published Threshold has been met");
                    _timesPublishedThresholdEvent.Set();
                }
            }
        }

        private void UpdateProcessorLoopsEvent()
        {
            lock (_processorLoopSyncObj)
            {
                if (_currentProcessorLoop >= _processorLoopThreshold)
                {
                    _testOutput.WriteLine("Telemetry Publishing Loop Threshold has been met");
                    _processorLoopThresholdEvent.Set();
                }
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
