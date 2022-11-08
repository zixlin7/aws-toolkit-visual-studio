using System;
using System.Threading;

using log4net;

namespace Amazon.AWSToolkit
{
    /// <summary>
    /// Represents a global mutex that controls inter-process execution of cleanup related to log files 
    /// </summary>
    public static class LoggingMutex
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LoggingMutex));

        public static IDisposable Acquire()
        {
            Mutex mutex = null;
            try
            {
                mutex = new Mutex(false, "Global\\VSToolkitLoggingMutex");
                // wait for one second and if is not acquired another process already has it
                var result = mutex.WaitOne(TimeSpan.FromSeconds(1), false);
                if (!result)
                {
                    throw new TimeoutException();
                }
            }
            catch (AbandonedMutexException ex)
            {
                // NOTE: This behavior is deliberate. The exception allows acquiring the mutex
                // and hence it needs to be released
                // Refer: https://learn.microsoft.com/en-us/dotnet/api/system.threading.abandonedmutexexception?view=net-6.0

                Logger.Error("Mutex was abandoned.", ex);
            }
            catch (TimeoutException ex)
            {
                Logger.Error("Acquiring mutex timed out.", ex);
                mutex?.Close();
                throw;
            }

            //release mutex if it was successfully acquired
            return new DisposingAction(() =>
            {
                mutex?.ReleaseMutex();
                mutex?.Close();
            });
        }
    }
}
