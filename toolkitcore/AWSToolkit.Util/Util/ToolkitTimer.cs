using System;
using System.Timers;

namespace Amazon.AWSToolkit.Util
{
    /// <summary>
    /// Encapsulates <see cref="System.Timers.Timer"/>
    /// </summary>
    public class ToolkitTimer : IToolkitTimer
    {
        private readonly Timer _timer = new Timer();

        public event EventHandler<ToolkitTimerElapsedEventArgs> Elapsed;

        public bool AutoReset
        {
            get => _timer.AutoReset;
            set => _timer.AutoReset = value;
        }

        public double Interval
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        public ToolkitTimer()
        {
            _timer.Elapsed += OnInternalTimerElapsed;
        }

        private void OnInternalTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Elapsed?.Invoke(sender, new ToolkitTimerElapsedEventArgs(e.SignalTime));
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Elapsed -= OnInternalTimerElapsed;
                _timer?.Dispose();
            }
        }
    }
}
