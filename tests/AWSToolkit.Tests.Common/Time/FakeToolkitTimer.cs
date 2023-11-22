using System;

using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.Tests.Common.Time
{
    public class FakeToolkitTimer : IToolkitTimer
    {
        public event EventHandler<ToolkitTimerElapsedEventArgs> Elapsed;

        public bool AutoReset { get; set; }
        public double Interval { get; set; }

        public bool IsStarted;

        public void Start()
        {
            IsStarted = true;
        }

        public void Stop()
        {
            IsStarted = false;
        }

        public void RaiseElapsed()
        {
            if (!AutoReset)
            {
                Stop();
            }
            Elapsed?.Invoke(this, new ToolkitTimerElapsedEventArgs());
        }

        public void Dispose()
        {
        }
    }
}
