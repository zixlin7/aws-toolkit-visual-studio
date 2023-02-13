using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.CloudFormation.Util
{
    public static class CloudFormationTelemetryExtensionMethods
    {
        public static void RecordCloudFormationDelete(this ToolkitContext toolkitContext, ActionResults result,
            AwsConnectionSettings awsConnectionSettings)
        {
            var data = result.CreateMetricData<CloudformationDelete>(awsConnectionSettings,
                toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();

            toolkitContext.TelemetryLogger.RecordCloudformationDelete(data);
        }
    }
}
