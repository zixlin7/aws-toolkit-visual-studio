using System;
using System.Diagnostics;
using System.Timers;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients
{
    /// <summary>
    /// Attempts to raise an event when a process exits.
    /// </summary>
    /// <remarks>
    /// <see cref="Process.Exited"/> does not always fire on its own, so we create a timer
    /// that periodically checks if the Process has terminated. This way we are able to get
    /// an indication that a process has stopped.
    ///
    /// This is used to know when the language server is not running, since the VS SDK does
    /// not send a signal for this.
    /// </remarks>
    public class ProcessWatcher : IDisposable
    {
        private const int _timerIntervalMs = 1000;

        public event EventHandler ProcessEnded;

        private readonly Process _process;
        private readonly Timer _timer = new Timer(_timerIntervalMs) { AutoReset = true, };

        private bool _raised = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="process">Process to watch for its termination. Caller is responsible for disposing this object.</param>
        public ProcessWatcher(Process process)
        {
            _process = process;
            _process.Exited += OnProcessExited;

            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            RaiseProcessEnded();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (_process.HasExited)
                {
                    _timer.Stop();
                    RaiseProcessEnded();
                }
            }
            catch (Exception)
            {
                // The process may not have started.
                // Back the timer off by 10%
                _timer.Interval *= 1.1;
            }
        }

        private void RaiseProcessEnded()
        {
            if (_raised)
            {
                return;
            }

            _raised = true;
            ProcessEnded?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _process.Exited -= OnProcessExited;
            _timer.Elapsed -= OnTimerElapsed;

            _timer.Dispose();
        }
    }
}
