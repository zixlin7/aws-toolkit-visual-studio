using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.SQS.Util
{
    public static class SqsTelemetryExtensionMethods
    {
        public static void RecordSqsCreateQueue(this ToolkitContext toolkitContext, ActionResults result,
            AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<SqsCreateQueue>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.SqsQueueType = SqsQueueType.Standard;

            toolkitContext.TelemetryLogger.RecordSqsCreateQueue(data);
        }

        public static void RecordSqsDeleteQueue(this ToolkitContext toolkitContext, ActionResults result,
            AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<SqsDeleteQueue>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordSqsDeleteQueue(data);
        }
    }
}
