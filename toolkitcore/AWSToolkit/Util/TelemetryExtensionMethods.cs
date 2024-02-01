using System;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Telemetry;

namespace Amazon.AWSToolkit.Util
{
    public static class TelemetryExtensionMethods
    {
        /// <summary>
        /// Utility method to create a metric object and pre-populate it with standardized fields.
        /// </summary>
        /// <typeparam name="T">Metric object to instantiate</typeparam>
        /// <param name="result">Operation result, used to populate some of the metric fields</param>
        public static T CreateMetricData<T>(this ActionResults result, AwsConnectionSettings awsConnectionSettings,
            IAwsServiceClientManager serviceClientManager) where T : BaseTelemetryEvent, new()
        {
            var metricData = new T();
            metricData.AwsAccount = awsConnectionSettings?.GetAccountId(serviceClientManager) ??
                                    MetadataValue.Invalid;
            metricData.AwsRegion = awsConnectionSettings?.Region?.Id ?? MetadataValue.Invalid;

            // add error metadata if failed result is encountered
            if (result.AsTelemetryResult().Equals(Result.Failed))
            {
                metricData.AddErrorMetadata(result?.Exception);
            }

            return metricData;
        }

        /// <summary>
        /// Utility method to create a metric object and pre-populate it with standardized fields.
        /// </summary>
        /// <typeparam name="T">Metric object to instantiate</typeparam>
        /// <param name="result">Operation result, used to populate some of the metric fields</param>
        public static T CreateMetricData<T>(this ActionResults result, string accountId, string regionId) where T : BaseTelemetryEvent, new()
        {
            var metricData = new T();
            metricData.AwsAccount = accountId;
            metricData.AwsRegion = regionId;

            // add error metadata if failed result is encountered
            if (result.AsTelemetryResult().Equals(Result.Failed))
            {
                metricData.AddErrorMetadata(result?.Exception);
            }

            return metricData;
        }

        /// <summary>
        /// Utility method to create a metric object and pre-populate it with standardized fields.
        /// </summary>
        /// <typeparam name="T">Metric object to instantiate</typeparam>
        /// <param name="result">Operation result, used to populate some of the metric fields</param>
        public static T CreateMetricData<T>(this TaskResult result) where T : BaseTelemetryEvent, new()
        {
            var metricData = new T();
            metricData.AwsAccount = MetadataValue.NotApplicable;
            metricData.AwsRegion = MetadataValue.NotApplicable;

            // add error metadata if failed result is encountered
            if (result != null && result.Status.AsTelemetryResult().Equals(Result.Failed))
            {
                metricData.AddErrorMetadata(result.Exception);
            }

            return metricData;
        }

        public static void AddErrorMetadata<T>(this T metricData, Exception exception) where T : BaseTelemetryEvent, new()
        {
            var errorMetadata = TelemetryHelper.DetermineErrorMetadata(exception);

            metricData.Reason = errorMetadata.Reason;
            metricData.ErrorCode = errorMetadata.ErrorCode;
            metricData.CausedBy = errorMetadata.CausedBy.ToString();
            metricData.HttpStatusCode = errorMetadata.HttpStatusCode;
            metricData.RequestId = errorMetadata.RequestId;
            metricData.RequestServiceType = errorMetadata.RequestServiceType;
        }
    }
}
