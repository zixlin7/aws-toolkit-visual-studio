using System;
using System.Timers;

namespace Amazon.AWSToolkit.Util
{
    /// <summary>
    /// Drop in replacement for System.Timers.Timer for systems that can be tested.
    /// Functions, etc from the Timer class can be added to the interface as they are needed.
    /// </summary>
    /// <remarks><seealso cref="System.Timers.Timer"/></remarks>
    public interface IToolkitTimer: IDisposable
    {
        event ElapsedEventHandler Elapsed;

        bool AutoReset { get; set; }
        double Interval { get; set; }

        void Start();
        void Stop();
    }
}