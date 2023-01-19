using System;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Telemetry;

namespace Amazon.AWSToolkit.Commands
{
    public class ToolkitCommandResult
    {
        public bool Cancelled { get; private set; } = false;

        public bool Succeeded { get; private set; } = false;

        public Exception Exception { get; private set; }

        public static ToolkitCommandResult CreateSucceeded()
        {
            return new ToolkitCommandResult() { Succeeded = true };
        }

        public static ToolkitCommandResult CreateCancelled()
        {
            return new ToolkitCommandResult() { Cancelled = true, Succeeded = false };
        }

        public static ToolkitCommandResult CreateFailed(Exception exception = null)
        {
            return new ToolkitCommandResult() { Succeeded = false, Exception = exception };
        }
    }

    public static class ToolkitCommandResultExtensionMethods
    {
        public static Result AsTelemetryResult(this ToolkitCommandResult result)
        {
            if (result == null)
            {
                return Result.Failed;
            }

            if (result.Succeeded)
            {
                return Result.Succeeded;
            }

            return result.Cancelled ? Result.Cancelled : Result.Failed;
        }

        /// <summary>
        /// Utility method to create a metric object and pre-populate it with standardized fields.
        /// </summary>
        /// <typeparam name="T">Metric object to instantiate</typeparam>
        /// <param name="result">Operation result, used to populate some of the metric fields</param>
        /// <param name="awsConnectionSettings">Used to determine the account and region</param>
        /// <param name="serviceClientManager">Used to determine the account</param>
        public static T CreateMetricData<T>(this ToolkitCommandResult result,
            AwsConnectionSettings awsConnectionSettings,
            IAwsServiceClientManager serviceClientManager)
            where T : BaseTelemetryEvent, new()
        {
            var metricData = new T();
            metricData.AwsAccount = awsConnectionSettings?.GetAccountId(serviceClientManager) ??
                                    MetadataValue.Invalid;
            metricData.AwsRegion = awsConnectionSettings?.Region?.Id ?? MetadataValue.Invalid;
            metricData.Reason = TelemetryHelper.GetMetricsReason(result.Exception);

            return metricData;
        }
    }
}
