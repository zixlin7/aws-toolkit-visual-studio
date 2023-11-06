using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Util;

namespace Amazon.AwsToolkit.CodeWhisperer.Telemetry
{
    public static class LspTelemetryExtensionMethods
    {
        internal static void RecordModifySetting(this ITelemetryLogger telemetryLogger, TaskResult result, string settingId, string state)
        {
            var data = result.CreateMetricData<AwsModifySetting>();
            data.SettingId = settingId;
            data.SettingState = state;
            telemetryLogger.RecordAwsModifySetting(data);
        }

        /// <summary>
        /// Utility method to create a metric object and pre-populate it with standardized fields.
        /// </summary>
        /// <typeparam name="T">Metric object to instantiate</typeparam>
        /// <param name="result">Operation result, used to populate some of the metric fields</param>
        private static T CreateMetricData<T>(this TaskResult result) where T : BaseTelemetryEvent, new()
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
    }
}
