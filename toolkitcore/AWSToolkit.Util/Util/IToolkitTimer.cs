using System;

namespace Amazon.AWSToolkit.Util
{
    public class ToolkitTimerElapsedEventArgs : EventArgs
    {
        /// <summary>
        /// Indicates when <see cref="IToolkitTimer.Elapsed"/> was raised.
        /// </summary>
        public DateTime SignalTime { get; }

        public ToolkitTimerElapsedEventArgs() : this(DateTime.Now)
        {
        }

        public ToolkitTimerElapsedEventArgs(DateTime signalTime)
        {
            SignalTime = signalTime;
        }
    }

    /// <summary>
    /// Drop in replacement for <see cref="System.Timers.Timer"/> for systems that can be tested.
    /// Functions, etc from the Timer class can be added to the interface as they are needed.
    /// </summary>
    public interface IToolkitTimer : IDisposable
    {
        event EventHandler<ToolkitTimerElapsedEventArgs> Elapsed;

        /// <summary>
        /// Whether or not the event repeatedly fires (true), or fires once (false).
        /// </summary>
        bool AutoReset { get; set; }

        /// <summary>
        /// Number of milliseconds before raising <see cref="Elapsed"/>
        /// </summary>
        double Interval { get; set; }

        void Start();
        void Stop();
    }
}
