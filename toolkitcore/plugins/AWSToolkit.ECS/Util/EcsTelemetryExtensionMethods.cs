using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Telemetry;

namespace Amazon.AWSToolkit.ECS.Util
{
    public static class EcsTelemetryExtensionMethods
    {
        public static void RecordEcrCreateRepository(this ToolkitContext toolkitContext,
           ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = CreateMetricData<EcrCreateRepository>(result, awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordEcrCreateRepository(data);
        }

        public static void RecordEcrDeleteRepository(this ToolkitContext toolkitContext,
        ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = CreateMetricData<EcrDeleteRepository>(result, awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordEcrDeleteRepository(data);
        }

        public static void RecordEcsDeleteCluster(this ToolkitContext toolkitContext,
            ActionResults result, AwsConnectionSettings awsConnectionSettings)
        {
            var data = CreateMetricData<EcsDeleteCluster>(result, awsConnectionSettings, toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordEcsDeleteCluster(data);
        }

        /// <summary>
        /// Utility method to create a metric object and pre-populate it with standardized fields.
        /// </summary>
        /// <typeparam name="T">Metric object to instantiate</typeparam>
        /// <param name="result">Operation result, used to populate some of the metric fields</param>
        private static T CreateMetricData<T>(ActionResults result, AwsConnectionSettings awsConnectionSettings, IAwsServiceClientManager serviceClientManager) where T : BaseTelemetryEvent, new()
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
