using System;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.Util
{
    /// <summary>
    /// Small "What's the current time?" wrapper. Use as a replacement to
    /// DateTime.Now and Task.Delay in places where you want tests to mess with time.
    /// </summary>
    public class TimeProvider
    {
        public virtual DateTime GetCurrentTime()
        {
            return DateTime.Now;
        }

        public virtual async Task Delay(int delayMs, CancellationToken cancellationToken)
        {
            await Task.Delay(delayMs, cancellationToken);
        }
    }
}