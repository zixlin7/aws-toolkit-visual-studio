using System;

using Amazon.Runtime;

namespace Amazon.AWSToolkit.Telemetry
{
    /// <summary>
    /// Misc Telemetry related functions
    /// </summary>
    public static class TelemetryHelper
    {
        /// <summary>
        /// Returns a value to use as the reason field for a metric.
        /// Not intended to surface user-identifiable details.
        /// </summary>
        public static string GetMetricsReason(Exception exception)
        {
            switch (exception)
            {
                case null:
                    return null;
                case AmazonServiceException awsException:
                    return awsException.ErrorCode;
                default:
                    return "Unknown";
            }
        }
    }
}
