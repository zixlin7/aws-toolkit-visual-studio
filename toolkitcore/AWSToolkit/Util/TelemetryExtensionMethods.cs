using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Clients;
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
            metricData.Reason = TelemetryHelper.GetMetricsReason(result.Exception);

            return metricData;
        }
    }
}
