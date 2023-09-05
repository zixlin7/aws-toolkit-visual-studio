using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.DynamoDB.Util
{
    public static class DynamoDbTelemetryExtensionMethods
    {
        public static void RecordDynamoDbEdit(this ToolkitContext toolkitContext, DynamoDbTarget target,
            ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = results.CreateMetricData<DynamodbEdit>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = results.AsTelemetryResult();
            data.DynamoDbTarget = target;

            toolkitContext.TelemetryLogger.RecordDynamodbEdit(data);
        }

        public static void RecordDynamoDbView(this ToolkitContext toolkitContext, DynamoDbTarget target,
            ActionResults results, AwsConnectionSettings awsConnectionSettings)
        {
            var data = results.CreateMetricData<DynamodbView>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = results.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordDynamodbView(data);
        }
    }
}
