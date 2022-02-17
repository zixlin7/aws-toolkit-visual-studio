using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Core;

namespace Amazon.AWSToolkit.Telemetry
{
    public static class TelemetryLoggerExtensionMethods
    {
        public static void InvokeAndRecord (this ITelemetryLogger telemetryLogger, Action work,
            Action<ITelemetryLogger> recordMetric)
        {
            try
            {
                work();
            }
            finally
            {
                recordMetric(telemetryLogger);
            }
        }
      
        public static async Task TimeAndRecord(this ITelemetryLogger telemetryLogger, Func<Task> work,
            Action<ITelemetryLogger, long> recordMetric)
        {
            Stopwatch stopwatch = new Stopwatch();

            try
            {
                stopwatch.Start();
                await work();
            }
            finally
            {
                stopwatch.Stop();
                recordMetric(telemetryLogger, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
