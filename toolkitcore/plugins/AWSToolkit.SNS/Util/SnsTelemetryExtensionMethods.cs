using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.SNS.Util
{
    public static class SnsTelemetryExtensionMethods
    {
        public static void RecordSnsCreateTopic(this ToolkitContext toolkitContext, ActionResults result,
            AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<SnsCreateTopic>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordSnsCreateTopic(data);
        }

        public static void RecordSnsDeleteTopic(this ToolkitContext toolkitContext, ActionResults result,
            AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<SnsDeleteTopic>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordSnsDeleteTopic(data);
        }

        public static void RecordSnsCreateSubscription(this ToolkitContext toolkitContext, ActionResults result,
            AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<SnsCreateSubscription>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordSnsCreateSubscription(data);
        }


        public static void RecordSnsDeleteSubscription(this ToolkitContext toolkitContext, ActionResults result, int count,
            AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<SnsDeleteSubscription>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.Value = count;

            toolkitContext.TelemetryLogger.RecordSnsDeleteSubscription(data);
        }

        public static void RecordSnsPublishMessage(this ToolkitContext toolkitContext, ActionResults result,
            AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<SnsPublishMessage>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordSnsPublishMessage(data);
        }
    }
}
