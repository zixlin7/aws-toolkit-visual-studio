using System;
using System.Linq;

using Amazon.AWSToolkit.Exceptions;
using Amazon.Runtime;

namespace Amazon.AWSToolkit.Telemetry
{
    /// <summary>
    /// Misc Telemetry related functions
    /// </summary>
    public static class TelemetryHelper
    {
        public const string UnknownReason = "Unknown";

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
                case ToolkitException toolkitException:
                    return ConcatenateReasonFragments(
                        toolkitException.ServiceErrorCode,
                        toolkitException.ServiceStatusCode,
                        toolkitException.Code);
                default:
                    if (exception.InnerException != null)
                    {
                        return GetMetricsReason(exception.InnerException);
                    }
                    return UnknownReason;
            }
        }

        public static string ConcatenateReasonFragments(params string[] fragments)
        {
            var reason = string.Join("-", fragments.Where(r => !string.IsNullOrWhiteSpace(r)));

            return string.IsNullOrWhiteSpace(reason) ? UnknownReason : reason;
        }
    }
}
