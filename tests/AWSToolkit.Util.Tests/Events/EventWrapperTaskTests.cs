using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Events;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Events
{
    public class EventWrapperTaskTests
    {
        private EventHandler<EventArgs> _testEvent;

        private bool handlerAdded;

        private bool handlerRemoved;

        private event EventHandler<EventArgs> TestEvent
        {
            add
            {
                _testEvent += value;
                handlerAdded = true;
            }
            remove
            {
                _testEvent -= value;
                handlerRemoved = true;
            }
        }

        private void OnTestEvent(EventArgs e = null)
        {
            _testEvent?.Invoke(this, e ?? EventArgs.Empty);
        }

        private void AssertHandlerAsExpected()
        {
            Assert.True(handlerAdded);
            Assert.True(handlerRemoved);
            Assert.Null(_testEvent);
        }

        [Fact]
        public async Task SetResultToValidValueTerminatesSuccessfullyAsync()
        {
            Assert.True(await EventWrapperTask.Create<EventArgs, bool>(
                handler => TestEvent += handler,
                () => OnTestEvent(),
                (sender, e, setResult) => setResult(true),
                handler => TestEvent -= handler));

            AssertHandlerAsExpected();
        }

        [Fact]
        public async Task StartThrowsExceptionRaisedOnAwaitAsync()
        {
            await Assert.ThrowsAsync<Exception>(() => EventWrapperTask.Create<EventArgs, bool>(
                handler => TestEvent += handler,
                () => throw new Exception("KABOOM!"),
                (sender, e, setResult) => setResult(true),
                handler => TestEvent -= handler));

            AssertHandlerAsExpected();
        }

        [Fact]
        public async Task EventHandlerThrowsExceptionRaisedOnAwaitAsync()
        {
            await Assert.ThrowsAsync<Exception>(() => EventWrapperTask.Create<EventArgs, bool>(
                handler => TestEvent += handler,
                () => OnTestEvent(),
                (sender, e, setResult) => throw new Exception("KABOOM!"),
                handler => TestEvent -= handler));

            AssertHandlerAsExpected();
        }

        [Fact]
        public async Task DoesNotTerminateUntilSetResultCalledAsync()
        {
            var actualCallCount = 0;
            var expectedCallCount = 3;

            System.Timers.ElapsedEventHandler elapsedEventHandler = null;
            var timer = new System.Timers.Timer(100)
            {
                AutoReset = true
            };

            Assert.Equal("works", await EventWrapperTask.Create<System.Timers.ElapsedEventArgs, string>(
                handler => timer.Elapsed += elapsedEventHandler =
                    EventWrapperTask.ToLegacyEventHandler<System.Timers.ElapsedEventHandler, System.Timers.ElapsedEventArgs>(handler),
                () => timer.Start(),
                (sender, e, setResult) =>
                {
                    timer.Stop();
                    if (++actualCallCount == expectedCallCount)
                    {
                        setResult("works");
                        return;
                    }
                    timer.Start();
                },
                handler => timer.Elapsed -= elapsedEventHandler));

            Assert.Equal(expectedCallCount, actualCallCount);
        }

        [Fact]
        public async Task CancellationTokenCancelsTaskAndRemovesHandler()
        {
            using (var cancelSource = new CancellationTokenSource(500))
            {
                var cancelToken = cancelSource.Token;

                await Assert.ThrowsAsync<TaskCanceledException>(() => EventWrapperTask.Create<EventArgs, bool>(
                    handler => TestEvent += handler,
                    () => OnTestEvent(),
                    (sender, e, setResult) => { },
                    handler => TestEvent -= handler,
                    cancelToken));
            }

            AssertHandlerAsExpected();
        }

        [Fact]
        public async Task StartNotRunWhenCancellationTokenAlreadyCanceled()
        {
            var startRun = false;
            var cancelToken = new CancellationToken(true);

            await Assert.ThrowsAsync<TaskCanceledException>(() => EventWrapperTask.Create<EventArgs, bool>(
                handler => TestEvent += handler,
                () => startRun = true,
                (sender, e, setResult) => { },
                handler => TestEvent -= handler,
                cancelToken));

            Assert.False(startRun);
            AssertHandlerAsExpected();
        }

        [Fact]
        public void MatchingLegacyHandlerToTEventArgsIsSuccessful()
        {
            void Handler(object sender, System.Timers.ElapsedEventArgs e) { }

            var wrapped = EventWrapperTask.ToLegacyEventHandler<System.Timers.ElapsedEventHandler, System.Timers.ElapsedEventArgs>(Handler);

            Assert.NotNull(wrapped);
        }

        [Fact]
        public void NullHandlerThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                EventWrapperTask.ToLegacyEventHandler<System.Timers.ElapsedEventHandler, System.Timers.ElapsedEventArgs>(null));
        }

        [Fact]
        public void MismatchedLegacyHandlerParamCountThrowsToolkitException()
        {
            void Handler(object sender, EventArgs e) { }

            Assert.Throws<InvalidCastException>(() =>
                EventWrapperTask.ToLegacyEventHandler<Action<object>, EventArgs>(Handler));

            Assert.Throws<InvalidCastException>(() =>
                EventWrapperTask.ToLegacyEventHandler<Action<object, EventArgs, object>, AssemblyLoadEventArgs>(Handler));
        }

        [Fact]
        public void MismatchedLegacyHandlerParamTypesThrowsToolkitException()
        {
            void Handler(object sender, EventArgs e) { }

            Assert.Throws<InvalidCastException>(() =>
                EventWrapperTask.ToLegacyEventHandler<Action<string, System.Timers.ElapsedEventArgs>, EventArgs>(Handler));

            Assert.Throws<InvalidCastException>(() =>
                EventWrapperTask.ToLegacyEventHandler<System.Timers.ElapsedEventHandler, EventArgs>(Handler));
        }
    }
}
