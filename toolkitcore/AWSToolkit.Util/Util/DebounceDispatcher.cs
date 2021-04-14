
using System;
using System.Timers;

namespace Amazon.AWSToolkit.Util
{
    /// <summary>
    /// Allows events to be Debounced.
    /// When an event is debounced, only the last event is fired, after
    /// a specified amount of time has passed since the last time it
    /// was called.
    ///
    /// Inspiration: https://weblog.west-wind.com/posts/2017/jul/02/debouncing-and-throttling-dispatcher-events
    /// </summary>
    public class DebounceDispatcher : IDisposable
    {
        private readonly object _syncLock = new object();
        private Timer _timer;

        public void Debounce(double intervalMs, Action<object> action, object param = null)
        {
            lock (_syncLock)
            {
                DisposeTimer();

                // Reset the timer by recreating it.
                // action will only fire if this method is not called within intervalMs.
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
