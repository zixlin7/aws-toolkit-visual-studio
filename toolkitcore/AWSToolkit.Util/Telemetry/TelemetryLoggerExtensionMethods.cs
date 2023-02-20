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
      
        public static async Task InvokeAndRecordAsync(this ITelemetryLogger telemetryLogger, Func<Task> work,
            Action<ITelemetryLogger> recordMetric)
        {
            try
            {
                await work();
            }
            finally
            {
                recordMetric(telemetryLogger);
            }
        }
      
        public static async Task TimeAndRecordAsync(this ITelemetryLogger telemetryLogger, Func<Task> work,
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

        public static void TimeAndRecord(this ITelemetryLogger telemetryLogger, Action work,
            Action<ITelemetryLogger, double> recordMetric)
        {
            Stopwatch stopwatch = new Stopwatch();

            try
            {
                stopwatch.Start();
                work();
            }
            finally
            {
                stopwatch.Stop();
                recordMetric(telemetryLogger, stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}
