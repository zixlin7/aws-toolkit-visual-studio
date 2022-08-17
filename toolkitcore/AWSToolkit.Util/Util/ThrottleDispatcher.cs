using System;
using System.Timers;

namespace Amazon.AWSToolkit.Util
{
    /// <summary>
    /// Allows events to be Throttled.
    /// When an event is throttled, it will only be called at most once within a requested interval.
    ///
    /// Inspiration: https://weblog.west-wind.com/posts/2017/jul/02/debouncing-and-throttling-dispatcher-events
    /// </summary>
    public class ThrottleDispatcher : IDisposable
    {
        private readonly object _syncLock = new object();
        private Timer _timer;
        private DateTime _timerStartedOn = DateTime.MinValue;

        public void Throttle(double intervalMs, Action<object> action, object param = null)
        {
            lock (_syncLock)
            {
                // Stop pending timer, if there is one
                DisposeTimer();

                var now = DateTime.UtcNow;

                // If the timer last ran within the span of the requested interval,
                // invoke the timer to run one interval after the previous run.
                //
                // Example: If the timer last ran 2 seconds ago, with a requested interval of 5 seconds,
                // run the next timer after 3 seconds instead of 5.
                //
                // The timer may fire with a newer action.
                var lastFiredMs = now.Subtract(_timerStartedOn).TotalMilliseconds;
                if (lastFiredMs < intervalMs)
                {
                    intervalMs -= lastFiredMs;
                }

                // Recreate the timer with the calculated delay.
                // Action will fire no sooner than intervalMs.
                _timer = new Timer()
                {
                    Interval = intervalMs,
                    AutoReset = false,
                    Enabled = false
                };

                _timer.Elapsed += (sender, args) =>
                {
                    lock (_syncLock)
                    {
                        if (_timer == null)
                        {
                            return;
                        }

                        DisposeTimer();
                    }

                    action.Invoke(param);
                };

                _timer.Start();
                _timerStartedOn = now;
            }
        }

        public void Dispose()
        {
            DisposeTimer();
        }

        /// <summary>
        /// Caller is responsible for thread-sync if necessary
        /// </summary>
        private void DisposeTimer()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }
    }
}
