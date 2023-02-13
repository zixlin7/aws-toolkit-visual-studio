using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Telemetry;

namespace Amazon.AWSToolkit.DynamoDB.Util
{
    public static class DynamoDbTelemetryExtensionMethods
    {
        public static void RecordDynamoDbEdit(this ToolkitContext toolkitContext, DynamoDbTarget target,
            ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = new DynamodbEdit()
            {
                AwsAccount =
                    awsConnectionSettings?.GetAccountId(toolkitContext.ServiceClientManager) ?? MetadataValue.Invalid,
                AwsRegion = awsConnectionSettings?.Region?.Id ?? MetadataValue.Invalid,
                DynamoDbTarget = target,
                Result = results.AsTelemetryResult(),
                Reason = TelemetryHelper.GetMetricsReason(results.Exception),
            };

            toolkitContext.TelemetryLogger.RecordDynamodbEdit(data);
        }

        public static void RecordDynamoDbView(this ToolkitContext toolkitContext, DynamoDbTarget target,
            ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = new DynamodbView()
            {
                AwsAccount =
                    awsConnectionSettings?.GetAccountId(toolkitContext.ServiceClientManager) ?? MetadataValue.Invalid,
                AwsRegion = awsConnectionSettings?.Region?.Id ?? MetadataValue.Invalid,
                DynamoDbTarget = target,
                Result = results.AsTelemetryResult(),
                Reason = TelemetryHelper.GetMetricsReason(results.Exception),
            };

            toolkitContext.TelemetryLogger.RecordDynamodbView(data);
        }
    }
}
